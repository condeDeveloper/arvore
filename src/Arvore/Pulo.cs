namespace Conde.Arvore;

/// <summary>
/// A lista de pulos (skip list), de William Pugh, 1990. Nao e arvore, e esta
/// aqui exatamente por isso.
///
/// Sao varias listas encadeadas ordenadas, empilhadas. A de baixo tem todos os
/// elementos; cada nivel acima tem, em media, metade dos do nivel de baixo. A
/// busca comeca no topo e desce: enquanto o proximo for menor que o procurado,
/// anda; senao, desce um nivel.
///
/// O que ela faz de notavel nao e o desempenho, que e o mesmo de uma arvore
/// balanceada. E o CODIGO. Nao ha rotacao, nao ha caso duplo, nao ha cor, nao ha
/// altura guardada. Inserir e achar o lugar e remendar ponteiros. Pugh escreveu
/// o artigo exatamente com esse argumento: a estrutura e mais simples de
/// implementar certo, e implementar certo e a parte dificil.
///
/// A altura de cada elemento vem de MOEDAS: joga-se cara ou coroa ate dar coroa,
/// e a quantidade de caras e a altura. A garantia e probabilistica, como na
/// treap, e pelo mesmo motivo: o sorteio nao depende da entrada, entao nao
/// existe sequencia adversaria.
/// </summary>
public sealed class Pulo<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private sealed class No(TChave chave, TValor valor, int niveis)
    {
        public TChave Chave = chave;
        public TValor Valor = valor;
        public readonly No?[] Proximo = new No?[niveis];
    }

    /// <summary>
    /// O teto de niveis. Com 32, uma lista de quatro bilhoes de elementos ainda
    /// cabe, porque cada nivel corta a quantidade pela metade.
    /// </summary>
    public const int TetoDeNiveis = 32;

    private readonly Random sorteio;
    private readonly No cabeca;
    private int niveis = 1;

    public Pulo(int semente = 20260102)
    {
        sorteio = new Random(semente);
        cabeca = new No(default!, default!, TetoDeNiveis);
    }

    public int Quantos { get; private set; }

    public long ComparacoesDaUltima { get; private set; }

    public long ComparacoesNoTotal { get; private set; }

    /// <summary>Quantos niveis a lista tem agora.</summary>
    public int Niveis => niveis;

    private int Comparar(TChave a, TChave b)
    {
        ComparacoesDaUltima++; ComparacoesNoTotal++;
        return a.CompareTo(b);
    }

    /// <summary>
    /// Cara ou coroa ate dar coroa. A moeda e honesta, entao metade dos
    /// elementos fica no nivel 1, um quarto no 2, e assim por diante.
    /// </summary>
    private int SortearNiveis()
    {
        var quantos = 1;
        while (quantos < TetoDeNiveis && sorteio.Next(2) == 0) quantos++;
        return quantos;
    }

    public bool Inserir(TChave chave, TValor valor)
    {
        ComparacoesDaUltima = 0;
        var anteriores = new No[TetoDeNiveis];
        var atual = cabeca;

        // Desce do topo guardando, em cada nivel, o ultimo no menor que a chave.
        for (var nivel = niveis - 1; nivel >= 0; nivel--)
        {
            while (atual.Proximo[nivel] is { } proximo && Comparar(proximo.Chave, chave) < 0)
                atual = proximo;
            anteriores[nivel] = atual;
        }

        var candidato = atual.Proximo[0];
        if (candidato is not null && Comparar(candidato.Chave, chave) == 0)
        {
            candidato.Valor = valor;
            return false;
        }

        var altura = SortearNiveis();
        if (altura > niveis)
        {
            for (var nivel = niveis; nivel < altura; nivel++) anteriores[nivel] = cabeca;
            niveis = altura;
        }

        var novo = new No(chave, valor, altura);
        for (var nivel = 0; nivel < altura; nivel++)
        {
            novo.Proximo[nivel] = anteriores[nivel].Proximo[nivel];
            anteriores[nivel].Proximo[nivel] = novo;
        }
        Quantos++;
        return true;
    }

    public bool Achar(TChave chave, out TValor valor)
    {
        ComparacoesDaUltima = 0;
        var atual = cabeca;
        for (var nivel = niveis - 1; nivel >= 0; nivel--)
            while (atual.Proximo[nivel] is { } proximo && Comparar(proximo.Chave, chave) < 0)
                atual = proximo;

        var candidato = atual.Proximo[0];
        if (candidato is not null && Comparar(candidato.Chave, chave) == 0)
        {
            valor = candidato.Valor;
            return true;
        }
        valor = default!;
        return false;
    }

    public bool Remover(TChave chave)
    {
        ComparacoesDaUltima = 0;
        var anteriores = new No[TetoDeNiveis];
        var atual = cabeca;
        for (var nivel = niveis - 1; nivel >= 0; nivel--)
        {
            while (atual.Proximo[nivel] is { } proximo && Comparar(proximo.Chave, chave) < 0)
                atual = proximo;
            anteriores[nivel] = atual;
        }

        var alvo = atual.Proximo[0];
        if (alvo is null || Comparar(alvo.Chave, chave) != 0) return false;

        for (var nivel = 0; nivel < niveis; nivel++)
        {
            if (!ReferenceEquals(anteriores[nivel].Proximo[nivel], alvo)) break;
            anteriores[nivel].Proximo[nivel] = alvo.Proximo[nivel];
        }

        // Niveis que ficaram vazios desaparecem. Sem isso a busca comecaria num
        // nivel que nao tem nada e pagaria por nada.
        while (niveis > 1 && cabeca.Proximo[niveis - 1] is null) niveis--;
        Quantos--;
        return true;
    }

    public IEnumerable<(TChave Chave, TValor Valor)> EmOrdem()
    {
        for (var no = cabeca.Proximo[0]; no is not null; no = no.Proximo[0])
            yield return (no.Chave, no.Valor);
    }

    /// <summary>
    /// A "altura" aqui e a quantidade de niveis menos um, para ficar comparavel
    /// com a das arvores.
    ///
    /// Vazia devolve -1 como as arvores, e nao zero: a lista sempre tem o nivel
    /// zero, mesmo sem nenhum elemento nele, e confundir "tem um nivel" com "tem
    /// um elemento" faria a estrutura vazia parecer ter altura.
    /// </summary>
    public int Altura => Quantos == 0 ? -1 : niveis - 1;

    /// <summary>Quantos nos ha em cada nivel.</summary>
    public IReadOnlyList<int> PorNivel()
    {
        var contagem = new int[niveis];
        for (var nivel = 0; nivel < niveis; nivel++)
            for (var no = cabeca.Proximo[nivel]; no is not null; no = no.Proximo[nivel])
                contagem[nivel]++;
        return contagem;
    }

    /// <summary>
    /// Verdadeiro quando todo nivel esta ordenado e e um SUBCONJUNTO do nivel de
    /// baixo.
    ///
    /// A segunda parte e a que pega o defeito de verdade. Um remendo de
    /// ponteiros que esquece um nivel deixa a lista respondendo certo pelo nivel
    /// zero, que tem todo mundo, e com um atalho apontando para um no que nao
    /// existe mais. A busca passa a pular elementos, e so as vezes.
    /// </summary>
    public bool Consistente()
    {
        var debaixo = new HashSet<TChave>();
        for (var nivel = 0; nivel < niveis; nivel++)
        {
            var neste = new HashSet<TChave>();
            TChave? anterior = default;
            var primeiro = true;
            for (var no = cabeca.Proximo[nivel]; no is not null; no = no.Proximo[nivel])
            {
                if (!primeiro && anterior!.CompareTo(no.Chave) >= 0) return false;
                if (nivel > 0 && !debaixo.Contains(no.Chave)) return false;
                neste.Add(no.Chave);
                anterior = no.Chave;
                primeiro = false;
            }
            debaixo = neste;
        }
        return true;
    }
}
