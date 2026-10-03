namespace Conde.Arvore;

/// <summary>
/// A arvore AVL, de Adelson-Velsky e Landis, 1962: a primeira estrutura de
/// dados auto-balanceada da historia.
///
/// A invariante e de uma linha: em todo no, as alturas das duas subarvores
/// diferem no maximo em um. Dela sai a garantia de altura, e a conta e bonita.
/// Seja N(h) a MENOR quantidade de nos de uma AVL de altura h. Uma arvore
/// minima de altura h tem uma subarvore minima de altura h-1 e outra de altura
/// h-2, entao
///
///     N(h) = 1 + N(h-1) + N(h-2)
///
/// que e a recorrencia de Fibonacci deslocada. Logo N(h) cresce como a razao
/// aurea elevada a h, e a altura e no maximo 1,44 log2(n). Esse 1,44 e o numero
/// que separa a AVL da rubro-negra, cujo limite e 2 log2(n+1).
///
/// O preco dessa altura menor e pago no REBALANCEAMENTO: a AVL rebalanceia mais
/// vezes, porque a condicao dela e mais apertada. A troca e classica e o medidor
/// deste repositorio a exibe: a AVL busca mais rapido e escreve mais.
/// </summary>
public sealed class Avl<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    internal sealed class No(TChave chave, TValor valor)
    {
        public TChave Chave = chave;
        public TValor Valor = valor;
        public No? Esquerda;
        public No? Direita;
        public int Altura;          // em arestas; folha tem 0
    }

    private No? raiz;

    public int Quantos { get; private set; }

    public long ComparacoesDaUltima { get; private set; }

    public long ComparacoesNoTotal { get; private set; }

    /// <summary>Quantas rotacoes foram feitas desde que a arvore existe.</summary>
    public long Rotacoes { get; private set; }

    private int Comparar(TChave a, TChave b)
    {
        ComparacoesDaUltima++; ComparacoesNoTotal++;
        return a.CompareTo(b);
    }

    private static int AlturaDe(No? no) => no?.Altura ?? -1;

    private static void Recalcular(No no) =>
        no.Altura = 1 + Math.Max(AlturaDe(no.Esquerda), AlturaDe(no.Direita));

    /// <summary>O quanto o no pende: positivo para a esquerda.</summary>
    internal static int Balanco(No no) => AlturaDe(no.Esquerda) - AlturaDe(no.Direita);

    public bool Inserir(TChave chave, TValor valor)
    {
        ComparacoesDaUltima = 0;
        var novo = false;
        raiz = Inserir(raiz, chave, valor, ref novo);
        if (novo) Quantos++;
        return novo;
    }

    private No Inserir(No? no, TChave chave, TValor valor, ref bool novo)
    {
        if (no is null) { novo = true; return new No(chave, valor); }
        var ordem = Comparar(chave, no.Chave);
        if (ordem == 0) { no.Valor = valor; return no; }
        if (ordem < 0) no.Esquerda = Inserir(no.Esquerda, chave, valor, ref novo);
        else no.Direita = Inserir(no.Direita, chave, valor, ref novo);
        return Equilibrar(no);
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

    public bool Remover(TChave chave)
    {
        ComparacoesDaUltima = 0;
        var achou = false;
        raiz = Remover(raiz, chave, ref achou);
        if (achou) Quantos--;
        return achou;
    }

    private No? Remover(No? no, TChave chave, ref bool achou)
    {
        if (no is null) return null;
        var ordem = Comparar(chave, no.Chave);
        if (ordem < 0) no.Esquerda = Remover(no.Esquerda, chave, ref achou);
        else if (ordem > 0) no.Direita = Remover(no.Direita, chave, ref achou);
        else
        {
            achou = true;
            if (no.Esquerda is null) return no.Direita;
            if (no.Direita is null) return no.Esquerda;
            var sucessor = no.Direita;
            while (sucessor.Esquerda is not null) sucessor = sucessor.Esquerda;
            no.Chave = sucessor.Chave;
            no.Valor = sucessor.Valor;
            var ignorado = false;
            no.Direita = Remover(no.Direita, sucessor.Chave, ref ignorado);
        }
        return Equilibrar(no);
    }

    /// <summary>
    /// Os quatro casos de desequilibrio, que na verdade sao dois com espelho.
    ///
    /// O caso simples e quando o no pende para o mesmo lado em que o filho
    /// pende: uma rotacao resolve. O caso duplo e quando pendem para lados
    /// OPOSTOS, e ai uma rotacao so troca o problema de lado; e preciso girar o
    /// filho primeiro para transformar no caso simples.
    ///
    /// Esquecer o caso duplo e o defeito classico: a arvore continua respondendo
    /// certo, so para de ficar balanceada, e o teste que so confere as respostas
    /// nao percebe. Por isso aqui a invariante e conferida depois de CADA
    /// operacao, e nao no fim.
    /// </summary>
    private No Equilibrar(No no)
    {
        Recalcular(no);
        var balanco = Balanco(no);

        if (balanco > 1)
        {
            // Pende para a esquerda. Se o filho esquerdo pende para a direita,
            // e o caso duplo.
            if (Balanco(no.Esquerda!) < 0) no.Esquerda = GirarParaEsquerda(no.Esquerda!);
            return GirarParaDireita(no);
        }
        if (balanco < -1)
        {
            if (Balanco(no.Direita!) > 0) no.Direita = GirarParaDireita(no.Direita!);
            return GirarParaEsquerda(no);
        }
        return no;
    }

    private No GirarParaDireita(No no)
    {
        Rotacoes++;
        var filho = no.Esquerda!;
        no.Esquerda = filho.Direita;
        filho.Direita = no;
        Recalcular(no);
        Recalcular(filho);
        return filho;
    }

    private No GirarParaEsquerda(No no)
    {
        Rotacoes++;
        var filho = no.Direita!;
        no.Direita = filho.Esquerda;
        filho.Esquerda = no;
        Recalcular(no);
        Recalcular(filho);
        return filho;
    }

    public IEnumerable<(TChave Chave, TValor Valor)> EmOrdem() => EmOrdem(raiz);

    private static IEnumerable<(TChave, TValor)> EmOrdem(No? no)
    {
        if (no is null) yield break;
        foreach (var x in EmOrdem(no.Esquerda)) yield return x;
        yield return (no.Chave, no.Valor);
        foreach (var x in EmOrdem(no.Direita)) yield return x;
    }

    public int Altura => AlturaDe(raiz);

    internal No? Raiz => raiz;

    /// <summary>
    /// Verdadeiro quando toda a arvore obedece a invariante AVL, e quando a
    /// altura guardada em cada no bate com a altura de verdade.
    ///
    /// A segunda parte importa tanto quanto a primeira: a altura e um CACHE, e
    /// um cache errado faz o balanceamento tomar decisoes certas sobre uma
    /// arvore que nao existe.
    /// </summary>
    public bool Equilibrada() => Conferir(raiz) >= -1;

    private static int Conferir(No? no)
    {
        if (no is null) return -1;
        var esquerda = Conferir(no.Esquerda);
        var direita = Conferir(no.Direita);
        if (esquerda == int.MinValue || direita == int.MinValue) return int.MinValue;
        if (Math.Abs(esquerda - direita) > 1) return int.MinValue;
        var altura = 1 + Math.Max(esquerda, direita);
        return altura == no.Altura ? altura : int.MinValue;
    }
}
