namespace Conde.Arvore;

/// <summary>
/// A arvore rubro-negra inclinada para a esquerda (LLRB), de Robert Sedgewick,
/// 2008.
///
/// Uma rubro-negra e uma arvore 2-3 disfarcada de arvore binaria. As ligacoes
/// VERMELHAS representam os nos de tres chaves da arvore 2-3; as PRETAS sao as
/// ligacoes de verdade. Dai saem as regras:
///
/// - a raiz e preta;
/// - nao existem duas vermelhas seguidas;
/// - todo caminho da raiz ate uma folha tem a MESMA quantidade de pretas.
///
/// A terceira e a que garante a altura: se todo caminho tem p pretas e nao ha
/// duas vermelhas seguidas, o caminho mais longo e no maximo o dobro do mais
/// curto, e a altura fica em 2 log2(n+1).
///
/// A variante de Sedgewick acrescenta uma restricao: toda ligacao vermelha
/// aponta para a ESQUERDA. Isso elimina metade dos casos e deixa a insercao em
/// tres linhas. O preco aparece na medida: a LLRB faz mais rotacoes que a
/// rubro-negra classica, porque ela precisa consertar a inclinacao mesmo quando
/// a arvore ja estaria valida pelas regras originais.
///
/// A remocao e a parte dificil, e nao da para fingir que nao e. Ela so funciona
/// porque desce mantendo um invariante forte: o no atual ou o filho para onde se
/// vai desce VERMELHO. Isso garante que, ao chegar na folha, ela pode ser
/// arrancada sem violar a contagem de pretas; o conserto todo acontece na
/// subida.
/// </summary>
public sealed class Rubro<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    internal sealed class No(TChave chave, TValor valor, bool vermelho)
    {
        public TChave Chave = chave;
        public TValor Valor = valor;
        public No? Esquerda;
        public No? Direita;
        public bool Vermelho = vermelho;
    }

    private No? raiz;

    public int Quantos { get; private set; }

    public long ComparacoesDaUltima { get; private set; }

    public long ComparacoesNoTotal { get; private set; }

    /// <summary>Quantas rotacoes foram feitas desde que a arvore existe.</summary>
    public long Rotacoes { get; private set; }

    /// <summary>Quantas trocas de cor foram feitas.</summary>
    public long Recoloracoes { get; private set; }

    private int Comparar(TChave a, TChave b)
    {
        ComparacoesDaUltima++; ComparacoesNoTotal++;
        return a.CompareTo(b);
    }

    private static bool Vermelho(No? no) => no is { Vermelho: true };

    public bool Inserir(TChave chave, TValor valor)
    {
        ComparacoesDaUltima = 0;
        var novo = false;
        raiz = Inserir(raiz, chave, valor, ref novo);
        raiz!.Vermelho = false;                 // a raiz e sempre preta
        if (novo) Quantos++;
        return novo;
    }

    private No Inserir(No? no, TChave chave, TValor valor, ref bool novo)
    {
        // Todo no nasce VERMELHO, que e o mesmo que dizer que ele entra junto do
        // pai, no no da arvore 2-3.
        if (no is null) { novo = true; return new No(chave, valor, true); }

        var ordem = Comparar(chave, no.Chave);
        if (ordem < 0) no.Esquerda = Inserir(no.Esquerda, chave, valor, ref novo);
        else if (ordem > 0) no.Direita = Inserir(no.Direita, chave, valor, ref novo);
        else no.Valor = valor;

        return Arrumar(no);
    }

    /// <summary>
    /// Os tres consertos, nesta ordem, que e a parte bonita da variante de
    /// Sedgewick: a insercao inteira cabe aqui.
    /// </summary>
    private No Arrumar(No no)
    {
        // Vermelha a direita e preta a esquerda: endireita.
        if (Vermelho(no.Direita) && !Vermelho(no.Esquerda)) no = GirarParaEsquerda(no);
        // Duas vermelhas seguidas a esquerda: gira para a direita.
        if (Vermelho(no.Esquerda) && Vermelho(no.Esquerda!.Esquerda)) no = GirarParaDireita(no);
        // Os dois filhos vermelhos: sobe a cor, que e o 4-no quebrando em dois.
        if (Vermelho(no.Esquerda) && Vermelho(no.Direita)) TrocarCores(no);
        return no;
    }

    private No GirarParaEsquerda(No no)
    {
        Rotacoes++;
        var filho = no.Direita!;
        no.Direita = filho.Esquerda;
        filho.Esquerda = no;
        filho.Vermelho = no.Vermelho;
        no.Vermelho = true;
        return filho;
    }

    private No GirarParaDireita(No no)
    {
        Rotacoes++;
        var filho = no.Esquerda!;
        no.Esquerda = filho.Direita;
        filho.Direita = no;
        filho.Vermelho = no.Vermelho;
        no.Vermelho = true;
        return filho;
    }

    private void TrocarCores(No no)
    {
        Recoloracoes++;
        no.Vermelho = !no.Vermelho;
        if (no.Esquerda is not null) no.Esquerda.Vermelho = !no.Esquerda.Vermelho;
        if (no.Direita is not null) no.Direita.Vermelho = !no.Direita.Vermelho;
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
        if (raiz is null) return false;
        if (!Achar(chave, out _)) return false;

        // A raiz precisa estar num 2-no ou 3-no para a descida comecar: se os
        // dois filhos forem pretos, ela vira vermelha temporariamente.
        if (!Vermelho(raiz.Esquerda) && !Vermelho(raiz.Direita)) raiz.Vermelho = true;
        raiz = Remover(raiz, chave);
        if (raiz is not null) raiz.Vermelho = false;
        Quantos--;
        return true;
    }

    private No? Remover(No no, TChave chave)
    {
        if (Comparar(chave, no.Chave) < 0)
        {
            if (!Vermelho(no.Esquerda) && !Vermelho(no.Esquerda?.Esquerda)) no = EmprestarAEsquerda(no);
            no.Esquerda = Remover(no.Esquerda!, chave);
        }
        else
        {
            if (Vermelho(no.Esquerda)) no = GirarParaDireita(no);
            // Chegou na folha procurada: arranca.
            if (Comparar(chave, no.Chave) == 0 && no.Direita is null) return null;
            if (!Vermelho(no.Direita) && !Vermelho(no.Direita?.Esquerda)) no = EmprestarADireita(no);
            if (Comparar(chave, no.Chave) == 0)
            {
                // Dois filhos: sobe o menor da direita e remove-o de la.
                var sucessor = Menor(no.Direita!);
                no.Chave = sucessor.Chave;
                no.Valor = sucessor.Valor;
                no.Direita = RemoverMenor(no.Direita!);
            }
            else
            {
                no.Direita = Remover(no.Direita!, chave);
            }
        }
        return Arrumar(no);
    }

    private static No Menor(No no)
    {
        while (no.Esquerda is not null) no = no.Esquerda;
        return no;
    }

    private No? RemoverMenor(No no)
    {
        if (no.Esquerda is null) return null;
        if (!Vermelho(no.Esquerda) && !Vermelho(no.Esquerda.Esquerda)) no = EmprestarAEsquerda(no);
        no.Esquerda = RemoverMenor(no.Esquerda!);
        return Arrumar(no);
    }

    /// <summary>
    /// Empurra uma ligacao vermelha para a esquerda, pegando emprestado do irmao
    /// se ele tiver sobrando.
    ///
    /// E a peca que faz a remocao funcionar: ela garante que nunca se desce para
    /// um no que nao tem o que ceder, e e por isso que o conserto na subida da
    /// conta.
    /// </summary>
    private No EmprestarAEsquerda(No no)
    {
        TrocarCores(no);
        if (Vermelho(no.Direita?.Esquerda))
        {
            no.Direita = GirarParaDireita(no.Direita!);
            no = GirarParaEsquerda(no);
            TrocarCores(no);
        }
        return no;
    }

    private No EmprestarADireita(No no)
    {
        TrocarCores(no);
        if (Vermelho(no.Esquerda?.Esquerda))
        {
            no = GirarParaDireita(no);
            TrocarCores(no);
        }
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

    public int Altura => AlturaDe(raiz);

    private static int AlturaDe(No? no) =>
        no is null ? -1 : 1 + Math.Max(AlturaDe(no.Esquerda), AlturaDe(no.Direita));

    /// <summary>
    /// A altura PRETA: quantas ligacoes pretas ha da raiz ate qualquer folha.
    ///
    /// Ela e a mesma em todo caminho, por invariante, e e dela que vem a
    /// garantia de altura.
    /// </summary>
    public int AlturaPreta()
    {
        var altura = 0;
        for (var no = raiz; no is not null; no = no.Esquerda)
            if (!no.Vermelho) altura++;
        return altura;
    }

    /// <summary>
    /// As quatro regras conferidas de uma vez. Devolve nulo quando esta tudo
    /// certo e a descricao do problema quando nao esta.
    /// </summary>
    public string? Violacao()
    {
        if (Vermelho(raiz)) return "a raiz esta vermelha";
        if (raiz is null) return null;

        string? erro = null;
        int Conferir(No? no)
        {
            if (no is null) return 0;
            if (Vermelho(no) && (Vermelho(no.Esquerda) || Vermelho(no.Direita)))
                erro ??= $"duas vermelhas seguidas em {no.Chave}";
            if (Vermelho(no.Direita))
                erro ??= $"ligacao vermelha a direita em {no.Chave}, e esta e uma LLRB";
            var esquerda = Conferir(no.Esquerda);
            var direita = Conferir(no.Direita);
            if (esquerda != direita)
                erro ??= $"alturas pretas {esquerda} e {direita} em {no.Chave}";
            return esquerda + (no.Vermelho ? 0 : 1);
        }

        Conferir(raiz);
        return erro;
    }
}
