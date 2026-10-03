namespace Conde.Arvore;

/// <summary>
/// A arvore de busca binaria SEM balanceamento nenhum.
///
/// Ela esta aqui como a linha de base, e como a demonstracao do problema. O
/// codigo e o mais simples possivel e as respostas sao todas certas; o que ela
/// nao tem e garantia de altura.
///
/// E isso nao e uma preocupacao teorica. Inserir chaves JA ORDENADAS, que e o
/// caso mais comum que existe (identificadores que crescem, registros
/// importados de um arquivo ordenado, carimbos de tempo), produz uma arvore que
/// e literalmente uma lista encadeada: altura n-1, busca O(n), e a estrutura
/// gasta mais memoria que um vetor para fazer pior que ele.
///
/// O medidor mostra a altura explodindo, e todas as outras estruturas deste
/// repositorio existem por causa dessa linha.
/// </summary>
public sealed class Busca<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private sealed class No(TChave chave, TValor valor)
    {
        public TChave Chave = chave;
        public TValor Valor = valor;
        public No? Esquerda;
        public No? Direita;
    }

    private No? raiz;

    public int Quantos { get; private set; }

    public long ComparacoesDaUltima { get; private set; }

    public long ComparacoesNoTotal { get; private set; }

    private int Comparar(TChave a, TChave b)
    {
        ComparacoesDaUltima++; ComparacoesNoTotal++;
        return a.CompareTo(b);
    }

    public bool Inserir(TChave chave, TValor valor)
    {
        ComparacoesDaUltima = 0;
        if (raiz is null) { raiz = new No(chave, valor); Quantos = 1; return true; }

        var atual = raiz;
        while (true)
        {
            var ordem = Comparar(chave, atual.Chave);
            if (ordem == 0) { atual.Valor = valor; return false; }
            if (ordem < 0)
            {
                if (atual.Esquerda is null) { atual.Esquerda = new No(chave, valor); break; }
                atual = atual.Esquerda;
            }
            else
            {
                if (atual.Direita is null) { atual.Direita = new No(chave, valor); break; }
                atual = atual.Direita;
            }
        }
        Quantos++;
        return true;
    }

    public bool Achar(TChave chave, out TValor valor)
    {
        ComparacoesDaUltima = 0;
        var atual = raiz;
        while (atual is not null)
        {
            var ordem = Comparar(chave, atual.Chave);
            if (ordem == 0) { valor = atual.Valor; return true; }
            atual = ordem < 0 ? atual.Esquerda : atual.Direita;
        }
        valor = default!;
        return false;
    }

    /// <summary>
    /// Remocao por substituicao pelo SUCESSOR, que e o jeito de Hibbard, de 1962.
    ///
    /// O caso de dois filhos e o unico interessante: troca-se a chave pela menor
    /// da subarvore direita e remove-se aquela, que por construcao tem no maximo
    /// um filho.
    ///
    /// Hibbard provou algo desagradavel sobre isso: usar SEMPRE o sucessor
    /// desequilibra a arvore com o tempo. Depois de muitas insercoes e remocoes
    /// alternadas, a altura media vai para raiz de n em vez de log n. A correcao
    /// e alternar entre sucessor e antecessor, e o medidor deste repositorio
    /// mostra a diferenca.
    /// </summary>
    public bool Remover(TChave chave)
    {
        ComparacoesDaUltima = 0;
        var antes = Quantos;
        raiz = Remover(raiz, chave);
        return Quantos < antes;
    }

    private No? Remover(No? no, TChave chave)
    {
        if (no is null) return null;
        var ordem = Comparar(chave, no.Chave);
        if (ordem < 0) { no.Esquerda = Remover(no.Esquerda, chave); return no; }
        if (ordem > 0) { no.Direita = Remover(no.Direita, chave); return no; }

        Quantos--;
        if (no.Esquerda is null) return no.Direita;
        if (no.Direita is null) return no.Esquerda;

        // Dois filhos: sobe o menor da direita.
        var sucessor = no.Direita;
        while (sucessor.Esquerda is not null) sucessor = sucessor.Esquerda;
        no.Chave = sucessor.Chave;
        no.Valor = sucessor.Valor;
        Quantos++;                                   // o Remover de baixo desconta
        no.Direita = Remover(no.Direita, sucessor.Chave);
        return no;
    }

    public IEnumerable<(TChave Chave, TValor Valor)> EmOrdem() => EmOrdem(raiz);

    private static IEnumerable<(TChave, TValor)> EmOrdem(No? no)
    {
        if (no is null) yield break;
        foreach (var x in EmOrdem(no.Esquerda)) yield return x;
        yield return (no.Chave, no.Valor);
        foreach (var x in EmOrdem(no.Direita)) yield return x;
    }

    /// <summary>
    /// A altura, com pilha PROPRIA e nao com recursao.
    ///
    /// Isto nao e preferencia de estilo. A primeira versao era recursiva, de uma
    /// linha, e ela estourou a pilha ao medir a altura desta arvore com cem mil
    /// chaves em ordem crescente: 24.081 quadros antes de o processo morrer.
    ///
    /// O erro e didatico, porque ele E o assunto do repositorio. Uma arvore
    /// degenerada nao fica so lenta: ela fica funda demais para qualquer
    /// algoritmo recursivo que ande nela, inclusive os que servem para
    /// diagnostica-la. Nas arvores balanceadas o problema nao existe, porque a
    /// profundidade e logaritmica, e por isso so esta classe precisou mudar.
    /// </summary>
    public int Altura
    {
        get
        {
            if (raiz is null) return -1;
            var maior = 0;
            var pilha = new Stack<(No No, int Fundo)>();
            pilha.Push((raiz, 0));
            while (pilha.Count > 0)
            {
                var (no, fundo) = pilha.Pop();
                if (fundo > maior) maior = fundo;
                if (no.Esquerda is not null) pilha.Push((no.Esquerda, fundo + 1));
                if (no.Direita is not null) pilha.Push((no.Direita, fundo + 1));
            }
            return maior;
        }
    }

    /// <summary>
    /// O comprimento medio de caminho ate a raiz, que e o custo esperado de uma
    /// busca bem-sucedida. A altura conta o pior caso; isto conta o tipico.
    ///
    /// Com pilha propria pelo mesmo motivo da altura.
    /// </summary>
    public double CaminhoMedio()
    {
        if (raiz is null) return 0;
        var soma = 0L;
        var quantos = 0;
        var pilha = new Stack<(No No, int Fundo)>();
        pilha.Push((raiz, 0));
        while (pilha.Count > 0)
        {
            var (no, fundo) = pilha.Pop();
            soma += fundo; quantos++;
            if (no.Esquerda is not null) pilha.Push((no.Esquerda, fundo + 1));
            if (no.Direita is not null) pilha.Push((no.Direita, fundo + 1));
        }
        return soma / (double)quantos;
    }
}
