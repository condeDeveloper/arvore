namespace Conde.Arvore;

/// <summary>
/// A treap, de Seidel e Aragon, 1996: uma arvore de busca pela chave e um monte
/// pela PRIORIDADE, que e sorteada.
///
/// A ideia e desconcertante de tao simples. Uma arvore de busca binaria montada
/// com chaves em ordem ALEATORIA tem altura esperada 3 log n, e isso e teorema
/// antigo. O problema e que ninguem controla a ordem de chegada das chaves. A
/// treap resolve nao controlando a ordem de chegada e sim sorteando uma
/// prioridade para cada chave: a arvore resultante tem exatamente a FORMA que
/// teria se as chaves tivessem chegado em ordem aleatoria, qualquer que tenha
/// sido a ordem real.
///
/// E o balanceamento nao vem de regra nenhuma. Nao ha caso a analisar, nao ha
/// altura guardada, nao ha cor. Ha so a propriedade de monte sendo restaurada
/// por rotacoes, e a aleatoriedade faz o resto.
///
/// O custo disso e honesto e vale dizer: a garantia e PROBABILISTICA. Uma AVL
/// nunca passa de 1,44 log n, aconteca o que acontecer; uma treap pode, em
/// principio, ficar alta, so que a chance disso e ridiculamente pequena e nao
/// depende da entrada, que e o que importa contra um adversario.
/// </summary>
public sealed class Treap<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    internal sealed class No(TChave chave, TValor valor, int prioridade)
    {
        public TChave Chave = chave;
        public TValor Valor = valor;
        public readonly int Prioridade = prioridade;
        public No? Esquerda;
        public No? Direita;
    }

    private readonly Random sorteio;
    private No? raiz;

    /// <summary>
    /// A semente e explicita para o teste poder reproduzir. Em producao ela
    /// viria de uma fonte de verdade, e a diferenca importa: e justamente a
    /// imprevisibilidade que tira do adversario a capacidade de montar a entrada
    /// que derruba a arvore.
    /// </summary>
    public Treap(int semente = 20260102) => sorteio = new Random(semente);

    public int Quantos { get; private set; }

    public long ComparacoesDaUltima { get; private set; }

    public long ComparacoesNoTotal { get; private set; }

    public long Rotacoes { get; private set; }

    private int Comparar(TChave a, TChave b)
    {
        ComparacoesDaUltima++; ComparacoesNoTotal++;
        return a.CompareTo(b);
    }

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
        if (no is null) { novo = true; return new No(chave, valor, sorteio.Next()); }

        var ordem = Comparar(chave, no.Chave);
        if (ordem == 0) { no.Valor = valor; return no; }

        if (ordem < 0)
        {
            no.Esquerda = Inserir(no.Esquerda, chave, valor, ref novo);
            // O filho so sobe se a prioridade dele for maior: e a propriedade de
            // monte sendo restaurada, e nao uma regra de balanceamento.
            if (no.Esquerda.Prioridade > no.Prioridade) no = GirarParaDireita(no);
        }
        else
        {
            no.Direita = Inserir(no.Direita, chave, valor, ref novo);
            if (no.Direita.Prioridade > no.Prioridade) no = GirarParaEsquerda(no);
        }
        return no;
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
    /// Remover e o inverso de inserir: em vez de subir o no ate o lugar, ele e
    /// AFUNDADO ate virar folha e entao cortado.
    ///
    /// A cada passo sobe o filho de maior prioridade, que e o unico que pode
    /// ficar no lugar sem quebrar o monte.
    /// </summary>
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
        if (ordem < 0) { no.Esquerda = Remover(no.Esquerda, chave, ref achou); return no; }
        if (ordem > 0) { no.Direita = Remover(no.Direita, chave, ref achou); return no; }

        achou = true;
        return Afundar(no);
    }

    private No? Afundar(No no)
    {
        if (no.Esquerda is null) return no.Direita;
        if (no.Direita is null) return no.Esquerda;

        if (no.Esquerda.Prioridade > no.Direita.Prioridade)
        {
            no = GirarParaDireita(no);
            no.Direita = Afundar(no.Direita!);
        }
        else
        {
            no = GirarParaEsquerda(no);
            no.Esquerda = Afundar(no.Esquerda!);
        }
        return no;
    }

    private No GirarParaDireita(No no)
    {
        Rotacoes++;
        var filho = no.Esquerda!;
        no.Esquerda = filho.Direita;
        filho.Direita = no;
        return filho;
    }

    private No GirarParaEsquerda(No no)
    {
        Rotacoes++;
        var filho = no.Direita!;
        no.Direita = filho.Esquerda;
        filho.Esquerda = no;
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

    private static int AlturaDe(No? no) =>
        no is null ? -1 : 1 + Math.Max(AlturaDe(no.Esquerda), AlturaDe(no.Direita));

    /// <summary>
    /// Verdadeiro quando a propriedade de MONTE vale: todo pai tem prioridade
    /// maior ou igual a dos filhos.
    ///
    /// E a unica invariante que esta estrutura tem, e por isso ela e conferida
    /// depois de cada operacao nos testes: se ela quebrar, a arvore continua
    /// respondendo certo e a altura deixa de ter garantia nenhuma.
    /// </summary>
    public bool Monte() => Monte(raiz);

    private static bool Monte(No? no)
    {
        if (no is null) return true;
        if (no.Esquerda is not null && no.Esquerda.Prioridade > no.Prioridade) return false;
        if (no.Direita is not null && no.Direita.Prioridade > no.Prioridade) return false;
        return Monte(no.Esquerda) && Monte(no.Direita);
    }
}
