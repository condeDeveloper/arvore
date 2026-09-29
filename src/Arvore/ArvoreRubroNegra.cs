namespace Conde.Arvore;

/// <summary>
/// Uma árvore rubro-negra — a estrutura que segura o <c>SortedDictionary</c>,
/// o <c>TreeMap</c> do Java e o <c>std::map</c> do C++.
/// </summary>
/// <remarks>
/// <para>
/// O problema que ela resolve: uma árvore de busca comum é ótima com dados
/// embaralhados e <b>vira uma lista</b> com dados ordenados. Inserir 1, 2, 3, 4…
/// numa árvore sem balanceamento dá uma corrente de um ramo só, e a busca que
/// custaria <c>log n</c> passa a custar <c>n</c>.
/// </para>
/// <para>
/// E dados ordenados não são caso raro: são o caso <b>comum</b>. Chaves que
/// chegam de um banco, de um arquivo, de um contador — quase tudo chega
/// ordenado, e é justamente aí que a árvore ingênua desaba.
/// </para>
/// <para>
/// A solução de Bayer (1972), rebatizada por Guibas e Sedgewick (1978), são
/// cinco regras. Cada nó é vermelho ou preto, e:
/// </para>
/// <list type="number">
///   <item><description>a raiz é preta;</description></item>
///   <item><description>as folhas (os nulos) são pretas;</description></item>
///   <item><description>um nó vermelho tem os dois filhos pretos — <b>nunca dois vermelhos seguidos</b>;</description></item>
///   <item><description>todo caminho da raiz a uma folha passa pelo <b>mesmo número</b> de nós pretos;</description></item>
///   <item><description>(a cor é uma informação só, um bit por nó.)</description></item>
/// </list>
/// <para>
/// As regras 3 e 4 juntas dão o resultado: o caminho mais longo tem <b>no
/// máximo o dobro</b> do mais curto. Isso basta para garantir altura
/// <c>O(log n)</c>, e é muito mais barato de manter que o equilíbrio perfeito de
/// uma AVL — que precisa de mais rotações para reequilibrar.
/// </para>
/// <para>
/// A parte que ninguém acerta de primeira é a <b>remoção</b>. A inserção tem
/// três casos; a remoção tem seis, e um deles empurra o problema para cima na
/// árvore. É por isso que este arquivo tem um verificador de invariantes: sem
/// ele, um caso de remoção errado produz uma árvore que continua funcionando —
/// e vai ficando desbalanceada em silêncio até a busca custar <c>n</c>.
/// </para>
/// </remarks>
public sealed class ArvoreRubroNegra<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private enum Cor
    {
        Vermelho,
        Preto,
    }

    private sealed class No(TChave chave, TValor valor)
    {
        public TChave Chave = chave;
        public TValor Valor = valor;

        // Um nó novo nasce vermelho: é a cor que NÃO muda a altura preta dos
        // caminhos, então inserir um vermelho só pode quebrar a regra 3 -- que
        // é a fácil de consertar.
        public Cor Cor = Cor.Vermelho;

        public No? Esquerda;
        public No? Direita;
        public No? Pai;
    }

    private No? _raiz;

    public int Quantos { get; private set; }

    /// <summary>
    /// Quantas rotações foram feitas desde o começo. Serve para medir.
    /// </summary>
    /// <remarks>
    /// A promessa de uma rubro-negra é que a inserção custa <b>no máximo duas</b>
    /// rotações e a remoção <b>no máximo três</b>, independentemente do tamanho.
    /// A ferramenta de medida confere isso em milhões de operações.
    /// </remarks>
    public long Rotacoes { get; private set; }

    public bool Contem(TChave chave) => Achar(chave) is not null;

    public bool TentarPegar(TChave chave, out TValor valor)
    {
        var no = Achar(chave);

        if (no is null)
        {
            valor = default!;

            return false;
        }

        valor = no.Valor;

        return true;
    }

    public TValor this[TChave chave]
    {
        get => TentarPegar(chave, out var valor)
            ? valor
            : throw new KeyNotFoundException($"não há a chave {chave}");

        set => Por(chave, value);
    }

    private No? Achar(TChave chave)
    {
        var atual = _raiz;

        while (atual is not null)
        {
            var comparacao = chave.CompareTo(atual.Chave);

            if (comparacao == 0)
            {
                return atual;
            }

            atual = comparacao < 0 ? atual.Esquerda : atual.Direita;
        }

        return null;
    }

    // -- inserção ----------------------------------------------------------

    public void Por(TChave chave, TValor valor)
    {
        No? pai = null;
        var atual = _raiz;

        while (atual is not null)
        {
            pai = atual;

            var comparacao = chave.CompareTo(atual.Chave);

            if (comparacao == 0)
            {
                atual.Valor = valor;

                return;
            }

            atual = comparacao < 0 ? atual.Esquerda : atual.Direita;
        }

        var novo = new No(chave, valor) { Pai = pai };

        if (pai is null)
        {
            _raiz = novo;
        }
        else if (chave.CompareTo(pai.Chave) < 0)
        {
            pai.Esquerda = novo;
        }
        else
        {
            pai.Direita = novo;
        }

        Quantos++;

        ConsertarDepoisDeInserir(novo);
    }

    /// <summary>
    /// Os três casos da inserção.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O nó novo é vermelho, então a única regra que ele pode quebrar é a 3:
    /// dois vermelhos seguidos. E há exatamente três formas de consertar,
    /// conforme a cor do <b>tio</b> — o outro filho do avô.
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     <b>Tio vermelho.</b> Pinta o pai e o tio de preto e o avô de
    ///     vermelho. Isso conserta aqui e pode quebrar lá em cima — daí o laço.
    ///     É o único caso que sobe.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Tio preto, e o nó está "por dentro".</b> Uma rotação transforma
    ///     neste caso no de baixo. É um passo de arrumação, não de conserto.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Tio preto, e o nó está "por fora".</b> Uma rotação no avô e uma
    ///     troca de cores resolve, e o laço <b>acaba</b>.
    ///   </description></item>
    /// </list>
    /// <para>
    /// Como só o caso 1 sobe e ele não rotaciona, a inserção faz no máximo
    /// <b>duas</b> rotações — as dos casos 2 e 3, uma vez cada.
    /// </para>
    /// </remarks>
    private void ConsertarDepoisDeInserir(No no)
    {
        while (no.Pai is { Cor: Cor.Vermelho })
        {
            var pai = no.Pai;
            var avo = pai.Pai!;

            var paiEhEsquerdo = pai == avo.Esquerda;
            var tio = paiEhEsquerdo ? avo.Direita : avo.Esquerda;

            if (tio is { Cor: Cor.Vermelho })
            {
                // Caso 1: repinta e sobe.
                pai.Cor = Cor.Preto;
                tio.Cor = Cor.Preto;
                avo.Cor = Cor.Vermelho;

                no = avo;

                continue;
            }

            // Caso 2: o nó está por dentro. Uma rotação o põe por fora.
            if (paiEhEsquerdo && no == pai.Direita)
            {
                no = pai;
                GirarParaEsquerda(no);
                pai = no.Pai!;
            }
            else if (!paiEhEsquerdo && no == pai.Esquerda)
            {
                no = pai;
                GirarParaDireita(no);
                pai = no.Pai!;
            }

            // Caso 3: por fora. Rotaciona o avô e acaba.
            pai.Cor = Cor.Preto;
            avo.Cor = Cor.Vermelho;

            if (paiEhEsquerdo)
            {
                GirarParaDireita(avo);
            }
            else
            {
                GirarParaEsquerda(avo);
            }
        }

        // A raiz é sempre preta. Pintá-la aqui, no fim, é mais simples que
        // tratar o caso dentro do laço -- e é seguro porque pintar a raiz de
        // preto aumenta a altura preta de TODOS os caminhos igualmente.
        _raiz!.Cor = Cor.Preto;
    }

    // -- remoção -----------------------------------------------------------

    public bool Remover(TChave chave)
    {
        var no = Achar(chave);

        if (no is null)
        {
            return false;
        }

        Remover(no);

        Quantos--;

        return true;
    }

    private void Remover(No no)
    {
        // Um nó com dois filhos não é removido: ele recebe a chave do sucessor,
        // e quem sai é o sucessor -- que tem no máximo um filho, por construção.
        // É o truque que reduz seis casos a três.
        if (no.Esquerda is not null && no.Direita is not null)
        {
            var sucessor = MenorDe(no.Direita);

            no.Chave = sucessor.Chave;
            no.Valor = sucessor.Valor;

            no = sucessor;
        }

        var filho = no.Esquerda ?? no.Direita;

        if (filho is not null)
        {
            // Um nó com um filho só: ele é preto e o filho é vermelho, sempre.
            // (Se fosse vermelho com um filho, a altura preta dos dois lados
            // seria diferente.) Então basta promover o filho e pintá-lo de
            // preto.
            Trocar(no, filho);

            filho.Cor = Cor.Preto;

            return;
        }

        if (no.Cor == Cor.Preto)
        {
            // Um preto sem filhos: tirar ele reduz a altura preta de um lado.
            // O conserto tem de acontecer ANTES de desligar, porque ele precisa
            // do irmão -- e o irmão se acha pelo pai.
            ConsertarDepoisDeRemover(no);
        }

        Trocar(no, null);
    }

    /// <summary>
    /// Os quatro casos da remoção — a parte que ninguém acerta de primeira.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tirar um nó preto deixa um caminho com um preto a menos, e a regra 4
    /// quebra. O conserto é uma cascata de casos sobre a cor do <b>irmão</b> e
    /// dos sobrinhos:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     <b>Irmão vermelho.</b> Uma rotação o troca por um irmão preto, e cai
    ///     num dos casos de baixo. É arrumação.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Irmão preto com dois sobrinhos pretos.</b> Pinta o irmão de
    ///     vermelho: agora os dois lados têm um preto a menos, o que é
    ///     equilibrado, e o problema <b>sobe</b> um nível.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Irmão preto com o sobrinho de fora preto.</b> Uma rotação no
    ///     irmão traz o sobrinho vermelho para fora, e cai no caso 4.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Irmão preto com o sobrinho de fora vermelho.</b> Uma rotação no
    ///     pai resolve, e o laço <b>acaba</b>.
    ///   </description></item>
    /// </list>
    /// <para>
    /// Só o caso 2 sobe, e ele não rotaciona. Os casos 1, 3 e 4 rotacionam uma
    /// vez cada e o 4 encerra — daí o teto de <b>três</b> rotações por remoção.
    /// </para>
    /// <para>
    /// Um caso escrito errado aqui não estoura nada: a árvore continua
    /// respondendo certo e vai ficando torta em silêncio. É exatamente por isso
    /// que o verificador de invariantes existe, e que os testes o rodam depois
    /// de <b>cada</b> operação.
    /// </para>
    /// </remarks>
    private void ConsertarDepoisDeRemover(No no)
    {
        while (no != _raiz && no.Cor == Cor.Preto)
        {
            var pai = no.Pai!;
            var ehEsquerdo = no == pai.Esquerda;

            var irmao = ehEsquerdo ? pai.Direita : pai.Esquerda;

            if (irmao is null)
            {
                // Não deveria acontecer numa árvore válida -- e se acontecer, é
                // melhor subir que desreferenciar nulo.
                no = pai;

                continue;
            }

            if (irmao.Cor == Cor.Vermelho)
            {
                // Caso 1: troca por um irmão preto.
                irmao.Cor = Cor.Preto;
                pai.Cor = Cor.Vermelho;

                if (ehEsquerdo)
                {
                    GirarParaEsquerda(pai);
                }
                else
                {
                    GirarParaDireita(pai);
                }

                irmao = ehEsquerdo ? pai.Direita! : pai.Esquerda!;
            }

            var deDentro = ehEsquerdo ? irmao.Esquerda : irmao.Direita;
            var deFora = ehEsquerdo ? irmao.Direita : irmao.Esquerda;

            if (EhPreto(deDentro) && EhPreto(deFora))
            {
                // Caso 2: o único que sobe.
                irmao.Cor = Cor.Vermelho;
                no = pai;

                continue;
            }

            if (EhPreto(deFora))
            {
                // Caso 3: traz o vermelho para fora.
                if (deDentro is not null)
                {
                    deDentro.Cor = Cor.Preto;
                }

                irmao.Cor = Cor.Vermelho;

                if (ehEsquerdo)
                {
                    GirarParaDireita(irmao);
                }
                else
                {
                    GirarParaEsquerda(irmao);
                }

                irmao = ehEsquerdo ? pai.Direita! : pai.Esquerda!;
                deFora = ehEsquerdo ? irmao.Direita : irmao.Esquerda;
            }

            // Caso 4: resolve e acaba.
            irmao.Cor = pai.Cor;
            pai.Cor = Cor.Preto;

            if (deFora is not null)
            {
                deFora.Cor = Cor.Preto;
            }

            if (ehEsquerdo)
            {
                GirarParaEsquerda(pai);
            }
            else
            {
                GirarParaDireita(pai);
            }

            no = _raiz!;
        }

        no.Cor = Cor.Preto;
    }

    private static bool EhPreto(No? no) => no is null || no.Cor == Cor.Preto;

    private static No MenorDe(No no)
    {
        while (no.Esquerda is not null)
        {
            no = no.Esquerda;
        }

        return no;
    }

    /// <summary>Põe <paramref name="novo"/> no lugar de <paramref name="velho"/>.</summary>
    private void Trocar(No velho, No? novo)
    {
        if (velho.Pai is null)
        {
            _raiz = novo;
        }
        else if (velho == velho.Pai.Esquerda)
        {
            velho.Pai.Esquerda = novo;
        }
        else
        {
            velho.Pai.Direita = novo;
        }

        if (novo is not null)
        {
            novo.Pai = velho.Pai;
        }
    }

    // -- rotações ----------------------------------------------------------

    /// <summary>
    /// A rotação: a operação que reequilibra sem quebrar a ordem.
    /// </summary>
    /// <remarks>
    /// <para>
    /// É a única coisa que uma árvore de busca pode fazer com a forma dela sem
    /// mexer no conteúdo, e o motivo é uma propriedade simples: numa árvore de
    /// busca, o percurso em ordem é a sequência ordenada das chaves, e a
    /// rotação <b>não muda esse percurso</b>.
    /// </para>
    /// <code>
    ///      x                y
    ///     / \              / \
    ///    a   y    ──►     x   c
    ///       / \          / \
    ///      b   c        a   b
    ///
    ///   a x b y c   =   a x b y c
    /// </code>
    /// <para>
    /// As duas árvores contêm a mesma coisa na mesma ordem, com alturas
    /// diferentes. Todo balanceamento — rubro-negra, AVL, splay, treap — é
    /// construído em cima disto.
    /// </para>
    /// </remarks>
    private void GirarParaEsquerda(No x)
    {
        var y = x.Direita!;

        x.Direita = y.Esquerda;

        if (y.Esquerda is not null)
        {
            y.Esquerda.Pai = x;
        }

        y.Pai = x.Pai;

        if (x.Pai is null)
        {
            _raiz = y;
        }
        else if (x == x.Pai.Esquerda)
        {
            x.Pai.Esquerda = y;
        }
        else
        {
            x.Pai.Direita = y;
        }

        y.Esquerda = x;
        x.Pai = y;

        Rotacoes++;
    }

    private void GirarParaDireita(No y)
    {
        var x = y.Esquerda!;

        y.Esquerda = x.Direita;

        if (x.Direita is not null)
        {
            x.Direita.Pai = y;
        }

        x.Pai = y.Pai;

        if (y.Pai is null)
        {
            _raiz = x;
        }
        else if (y == y.Pai.Esquerda)
        {
            y.Pai.Esquerda = x;
        }
        else
        {
            y.Pai.Direita = x;
        }

        x.Direita = y;
        y.Pai = x;

        Rotacoes++;
    }

    // -- percurso ----------------------------------------------------------

    /// <summary>Os pares, em ordem de chave.</summary>
    public IEnumerable<KeyValuePair<TChave, TValor>> EmOrdem()
    {
        // Uma pilha explícita em vez de recursão: uma árvore com um milhão de
        // nós tem altura 40, o que caberia -- e uma árvore QUEBRADA pode ter
        // altura um milhão, e aí o percurso derrubaria o processo em vez de o
        // teste acusar.
        var pilha = new Stack<No>();
        var atual = _raiz;

        while (atual is not null || pilha.Count > 0)
        {
            while (atual is not null)
            {
                pilha.Push(atual);
                atual = atual.Esquerda;
            }

            atual = pilha.Pop();

            yield return new KeyValuePair<TChave, TValor>(atual.Chave, atual.Valor);

            atual = atual.Direita;
        }
    }

    public int Altura() => Altura(_raiz);

    private static int Altura(No? no) =>
        no is null ? 0 : 1 + Math.Max(Altura(no.Esquerda), Altura(no.Direita));

    /// <summary>A altura preta: quantos nós pretos há da raiz até uma folha.</summary>
    public int AlturaPreta()
    {
        var quantos = 0;
        var atual = _raiz;

        while (atual is not null)
        {
            if (atual.Cor == Cor.Preto)
            {
                quantos++;
            }

            atual = atual.Esquerda;
        }

        return quantos + 1;
    }

    // -- o verificador -----------------------------------------------------

    /// <summary>
    /// Confere as cinco regras, e devolve o que estiver quebrado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta é a peça mais importante do projeto, e a razão é que uma árvore
    /// rubro-negra com um caso de remoção errado <b>continua funcionando</b>.
    /// Ela guarda, busca e devolve tudo certo; só vai ficando torta, e a busca
    /// que custava 20 passos passa a custar mil.
    /// </para>
    /// <para>
    /// Sem o verificador, o teste passa e o defeito fica. Com ele, rodado depois
    /// de <b>cada</b> operação, um caso errado aparece na hora em que acontece —
    /// e não num relatório de lentidão seis meses depois.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Conferir()
    {
        var problemas = new List<string>();

        if (_raiz is { Cor: Cor.Vermelho })
        {
            problemas.Add("a raiz é vermelha (regra 1)");
        }

        if (_raiz is not null && _raiz.Pai is not null)
        {
            problemas.Add("a raiz tem pai");
        }

        var contados = 0;

        Conferir(_raiz, null, problemas, ref contados);

        if (contados != Quantos)
        {
            problemas.Add($"a contagem diz {Quantos} e há {contados} nós");
        }

        // A ordem: o percurso em ordem tem de ser crescente. É a propriedade de
        // árvore de busca, independente do balanceamento.
        TChave? anterior = default;
        var primeiro = true;

        foreach (var par in EmOrdem())
        {
            if (!primeiro && anterior!.CompareTo(par.Key) >= 0)
            {
                problemas.Add($"fora de ordem: {anterior} vem antes de {par.Key}");
            }

            anterior = par.Key;
            primeiro = false;
        }

        // A altura: as regras 3 e 4 juntas garantem no máximo 2·log₂(n+1).
        if (Quantos > 0)
        {
            var teto = 2 * (int)Math.Ceiling(Math.Log2(Quantos + 1));

            if (Altura() > teto)
            {
                problemas.Add($"altura {Altura()} passa do teto de {teto} para {Quantos} nós");
            }
        }

        return problemas;
    }

    private static int Conferir(No? no, No? pai, List<string> problemas, ref int contados)
    {
        if (no is null)
        {
            // Um nulo é preto e conta 1 na altura preta.
            return 1;
        }

        contados++;

        if (no.Pai != pai)
        {
            problemas.Add($"o pai de {no.Chave} está errado");
        }

        // Regra 3: um vermelho não pode ter filho vermelho.
        if (no.Cor == Cor.Vermelho)
        {
            if (no.Esquerda is { Cor: Cor.Vermelho } || no.Direita is { Cor: Cor.Vermelho })
            {
                problemas.Add($"dois vermelhos seguidos em {no.Chave} (regra 3)");
            }
        }

        var esquerda = Conferir(no.Esquerda, no, problemas, ref contados);
        var direita = Conferir(no.Direita, no, problemas, ref contados);

        // Regra 4: os dois lados têm a mesma altura preta.
        if (esquerda != direita)
        {
            problemas.Add(
                $"altura preta diferente em {no.Chave}: {esquerda} à esquerda "
                + $"e {direita} à direita (regra 4)");
        }

        return esquerda + (no.Cor == Cor.Preto ? 1 : 0);
    }

    public void Limpar()
    {
        _raiz = null;
        Quantos = 0;
    }
}
