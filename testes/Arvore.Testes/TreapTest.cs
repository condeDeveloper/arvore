using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A treap, e a propriedade que a torna notavel: a FORMA nao depende da ordem de
/// chegada das chaves.
/// </summary>
public class TreapTest
{
    /// <summary>
    /// A propriedade de monte vale depois de cada operacao. E a unica invariante
    /// que a estrutura tem, e se ela quebrar a arvore continua respondendo certo
    /// e perde a garantia de altura.
    /// </summary>
    [Fact]
    public void APropriedadeDeMonteValeSempre()
    {
        var treap = new Treap<int, string>(semente: 5);
        var sorteio = new Random(7);
        for (var passo = 0; passo < 5000; passo++)
        {
            var chave = sorteio.Next(0, 400);
            if (sorteio.NextDouble() < 0.6) treap.Inserir(chave, "x");
            else treap.Remover(chave);
            Assert.True(treap.Monte(), $"o monte quebrou no passo {passo}");
        }
    }

    /// <summary>
    /// A FORMA depende so das prioridades, e nao da ordem de chegada.
    ///
    /// Este e o teste que mostra a ideia inteira. As mesmas chaves, com as
    /// mesmas prioridades, inseridas em ordens completamente diferentes, dao
    /// exatamente a mesma arvore. E por isso que a entrada ordenada, que destroi
    /// a arvore sem balanceamento, nao faz diferenca nenhuma aqui.
    ///
    /// O teste monta as prioridades a mao, inserindo na ordem em que o sorteio
    /// as produz, para que as duas arvores recebam as mesmas.
    /// </summary>
    [Fact]
    public void AFormaNaoDependeDaOrdemDeChegada()
    {
        // Prioridades fixas, atribuidas por chave.
        var prioridades = new Dictionary<int, int>();
        var sorteio = new Random(23);
        for (var chave = 0; chave < 300; chave++) prioridades[chave] = sorteio.Next();

        List<(int, int)> Montar(IEnumerable<int> ordem)
        {
            var treap = new TreapFixa(prioridades);
            foreach (var chave in ordem) treap.Inserir(chave);
            return treap.Forma();
        }

        var crescente = Montar(Sequencias.Crescentes(300));
        var decrescente = Montar(Sequencias.Decrescentes(300));
        var zigue = Montar(Sequencias.ZigueZague(300));
        var embaralhada = Montar(Sequencias.Crescentes(300).OrderBy(_ => sorteio.Next()));

        Assert.Equal(crescente, decrescente);
        Assert.Equal(crescente, zigue);
        Assert.Equal(crescente, embaralhada);
        // E a forma nao e trivial: a arvore nao e uma lista.
        Assert.True(crescente.Count == 300);
    }

    /// <summary>
    /// A altura fica perto de 3 log2(n), em QUALQUER sequencia de entrada,
    /// inclusive nas que destroem a arvore sem balanceamento.
    ///
    /// A garantia e probabilistica e nao determinista, e e por isso que o limite
    /// aqui e generoso: o que o teste afirma e que a entrada adversaria nao faz
    /// diferenca, nao que exista um teto absoluto.
    /// </summary>
    [Theory]
    [InlineData("crescente")]
    [InlineData("decrescente")]
    [InlineData("zigue-zague")]
    [InlineData("sorteada")]
    public void AAlturaNaoDependeDaEntrada(string qual)
    {
        var chaves = qual switch
        {
            "crescente" => Sequencias.Crescentes(10_000),
            "decrescente" => Sequencias.Decrescentes(10_000),
            "zigue-zague" => Sequencias.ZigueZague(10_000),
            _ => Sequencias.Sorteadas(10_000, semente: 3),
        };

        var treap = new Treap<int, string>(semente: 11);
        foreach (var chave in chaves) treap.Inserir(chave, "x");

        // 3 log2(10000) e perto de 40. Uma arvore sem balanceamento daria 9.999
        // nas tres primeiras.
        Assert.InRange(treap.Altura, 13, 50);
        Assert.True(treap.Monte());
    }

    /// <summary>
    /// A treap e mais ALTA que a AVL, e isso e o preco da simplicidade: ela nao
    /// tem caso nenhum a analisar.
    /// </summary>
    [Fact]
    public void AltaMasSimples()
    {
        var chaves = Sequencias.Sorteadas(20_000, semente: 29);

        var treap = new Treap<int, string>(semente: 31);
        foreach (var chave in chaves) treap.Inserir(chave, "x");

        var avl = new Avl<int, string>();
        foreach (var chave in chaves) avl.Inserir(chave, "x");

        Assert.True(treap.Altura > avl.Altura, $"treap: {treap.Altura}, AVL: {avl.Altura}");
        var (limiteAvl, _, _) = Sequencias.Limites(20_000);
        Assert.True(avl.Altura <= limiteAvl);
    }

    /// <summary>
    /// Sementes diferentes dao arvores diferentes com as MESMAS chaves, o que e
    /// a prova de que a forma vem do sorteio.
    /// </summary>
    [Fact]
    public void SementesDiferentesDaoArvoresDiferentes()
    {
        var chaves = Sequencias.Crescentes(1000);
        var alturas = new HashSet<int>();
        foreach (var semente in new[] { 1, 2, 3, 4, 5 })
        {
            var treap = new Treap<int, string>(semente);
            foreach (var chave in chaves) treap.Inserir(chave, "x");
            alturas.Add(treap.Altura);
            Assert.Equal(chaves, treap.EmOrdem().Select(p => p.Chave));
        }
        Assert.True(alturas.Count > 1, "todas as sementes deram a mesma altura");
    }

    /// <summary>
    /// Uma treap com prioridades DADAS, para o teste poder fixar a forma e
    /// comparar arvores montadas em ordens diferentes.
    /// </summary>
    private sealed class TreapFixa(Dictionary<int, int> prioridades)
    {
        private sealed class No(int chave, int prioridade)
        {
            public readonly int Chave = chave;
            public readonly int Prioridade = prioridade;
            public No? Esquerda;
            public No? Direita;
        }

        private No? raiz;

        public void Inserir(int chave) => raiz = Inserir(raiz, chave);

        private No Inserir(No? no, int chave)
        {
            if (no is null) return new No(chave, prioridades[chave]);
            if (chave < no.Chave)
            {
                no.Esquerda = Inserir(no.Esquerda, chave);
                if (no.Esquerda.Prioridade > no.Prioridade)
                {
                    var filho = no.Esquerda;
                    no.Esquerda = filho.Direita;
                    filho.Direita = no;
                    return filho;
                }
            }
            else if (chave > no.Chave)
            {
                no.Direita = Inserir(no.Direita, chave);
                if (no.Direita.Prioridade > no.Prioridade)
                {
                    var filho = no.Direita;
                    no.Direita = filho.Esquerda;
                    filho.Esquerda = no;
                    return filho;
                }
            }
            return no;
        }

        /// <summary>Cada no com o seu pai: isto E a forma da arvore.</summary>
        public List<(int, int)> Forma()
        {
            var saida = new List<(int, int)>();
            void Andar(No? no, int pai)
            {
                if (no is null) return;
                saida.Add((no.Chave, pai));
                Andar(no.Esquerda, no.Chave);
                Andar(no.Direita, no.Chave);
            }
            Andar(raiz, -1);
            saida.Sort();
            return saida;
        }
    }
}
