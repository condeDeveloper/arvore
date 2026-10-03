using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A bateria que TODA estrutura precisa passar, contra o juiz, depois de CADA
/// operacao.
///
/// Conferir so no fim nao serve. Um dicionario ordenado errado quase sempre
/// responde certo: ele devolve as chaves, na ordem, com os valores certos, e o
/// que quebrou foi a invariante. Comparar o resultado final pega o erro quando
/// ele ja virou resposta errada; conferir a cada passo pega a hora em que ele
/// nasce.
/// </summary>
public class ContratoTest
{
    public static TheoryData<string> Estruturas() =>
        ["busca", "avl", "rubro", "treap", "pulo"];

    internal static IDicionario<int, string> Criar(string nome) => nome switch
    {
        "busca" => new Busca<int, string>(),
        "avl" => new Avl<int, string>(),
        "rubro" => new Rubro<int, string>(),
        "treap" => new Treap<int, string>(semente: 7),
        "pulo" => new Pulo<int, string>(semente: 7),
        _ => throw new ArgumentOutOfRangeException(nameof(nome)),
    };

    /// <summary>Confere a invariante propria de cada estrutura.</summary>
    internal static void Invariante(IDicionario<int, string> estrutura)
    {
        switch (estrutura)
        {
            case Avl<int, string> avl:
                Assert.True(avl.Equilibrada(), "a AVL perdeu o equilibrio");
                break;
            case Rubro<int, string> rubro:
                Assert.Null(rubro.Violacao());
                break;
            case Treap<int, string> treap:
                Assert.True(treap.Monte(), "a treap perdeu a propriedade de monte");
                break;
            case Pulo<int, string> pulo:
                Assert.True(pulo.Consistente(), "a lista de pulos ficou inconsistente");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(Estruturas))]
    public void VaziaRespondeComoVazia(string nome)
    {
        var estrutura = Criar(nome);
        Assert.Equal(0, estrutura.Quantos);
        Assert.Empty(estrutura.EmOrdem());
        Assert.False(estrutura.Achar(42, out _));
        Assert.False(estrutura.Remover(42));
        Assert.Equal(-1, estrutura.Altura);
    }

    /// <summary>
    /// Mil operacoes sorteadas, conferindo contra o juiz e a invariante depois
    /// de cada uma.
    /// </summary>
    [Theory]
    [MemberData(nameof(Estruturas))]
    public void ConcordaComOJuizOperacaoPorOperacao(string nome)
    {
        var estrutura = Criar(nome);
        var juiz = new Juiz<int, string>();
        var sorteio = new Random(31);

        for (var passo = 0; passo < 1500; passo++)
        {
            var chave = sorteio.Next(0, 200);
            var sorte = sorteio.NextDouble();

            if (sorte < 0.5)
            {
                var valor = $"v{passo}";
                Assert.Equal(juiz.Inserir(chave, valor), estrutura.Inserir(chave, valor));
            }
            else if (sorte < 0.8)
            {
                Assert.Equal(juiz.Remover(chave), estrutura.Remover(chave));
            }
            else
            {
                var achouJuiz = juiz.Achar(chave, out var esperado);
                Assert.Equal(achouJuiz, estrutura.Achar(chave, out var obtido));
                if (achouJuiz) Assert.Equal(esperado, obtido);
            }

            Assert.Equal(juiz.Quantos, estrutura.Quantos);
            Assert.Equal(juiz.EmOrdem(), estrutura.EmOrdem());
            Invariante(estrutura);
        }
    }

    /// <summary>
    /// A travessia em ordem sai ORDENADA e sem repeticao, sempre. E o contrato
    /// que separa um dicionario ordenado de uma tabela de espalhamento.
    /// </summary>
    [Theory]
    [MemberData(nameof(Estruturas))]
    public void APercursoSaiOrdenadoESemRepetir(string nome)
    {
        var estrutura = Criar(nome);
        foreach (var chave in Sequencias.Sorteadas(500, semente: 13))
            estrutura.Inserir(chave, $"v{chave}");

        var chaves = estrutura.EmOrdem().Select(p => p.Chave).ToList();
        Assert.Equal(500, chaves.Count);
        Assert.Equal(chaves.OrderBy(x => x), chaves);
        Assert.Equal(chaves.Count, chaves.Distinct().Count());
    }

    /// <summary>
    /// Inserir a mesma chave duas vezes SUBSTITUI o valor e nao aumenta a
    /// contagem. E o caso que uma implementacao desatenta deixa passar e que
    /// produz duas entradas com a mesma chave na travessia.
    /// </summary>
    [Theory]
    [MemberData(nameof(Estruturas))]
    public void InserirDeNovoSubstituiSemDuplicar(string nome)
    {
        var estrutura = Criar(nome);
        for (var i = 0; i < 50; i++) Assert.True(estrutura.Inserir(i, $"a{i}"));
        for (var i = 0; i < 50; i++) Assert.False(estrutura.Inserir(i, $"b{i}"));

        Assert.Equal(50, estrutura.Quantos);
        Assert.Equal(50, estrutura.EmOrdem().Count());
        foreach (var (chave, valor) in estrutura.EmOrdem()) Assert.Equal($"b{chave}", valor);
        Invariante(estrutura);
    }

    /// <summary>
    /// Remover tudo esvazia, em qualquer ordem de remocao. A ordem embaralhada
    /// e de proposito: e nela que a remocao erra.
    /// </summary>
    [Theory]
    [MemberData(nameof(Estruturas))]
    public void RemoverTudoEsvazia(string nome)
    {
        var estrutura = Criar(nome);
        var chaves = Sequencias.Sorteadas(400, semente: 17);
        foreach (var chave in chaves) estrutura.Inserir(chave, "x");

        var sorteio = new Random(19);
        foreach (var chave in chaves.OrderBy(_ => sorteio.Next()))
        {
            Assert.True(estrutura.Remover(chave));
            Invariante(estrutura);
        }

        Assert.Equal(0, estrutura.Quantos);
        Assert.Empty(estrutura.EmOrdem());
        Assert.Equal(-1, estrutura.Altura);
    }

    /// <summary>
    /// Remover chave que nao existe nao mexe em nada. Parece bobo e e o caso que
    /// derruba uma remocao escrita de forma apressada: ela desce a arvore
    /// fazendo os consertos do caminho e chega na folha sem achar nada.
    /// </summary>
    [Theory]
    [MemberData(nameof(Estruturas))]
    public void RemoverOQueNaoExisteNaoMexeEmNada(string nome)
    {
        var estrutura = Criar(nome);
        for (var i = 0; i < 100; i += 2) estrutura.Inserir(i, $"v{i}");
        var antes = estrutura.EmOrdem().ToList();

        for (var i = 1; i < 100; i += 2) Assert.False(estrutura.Remover(i));
        Assert.False(estrutura.Remover(-1));
        Assert.False(estrutura.Remover(1000));

        Assert.Equal(antes, estrutura.EmOrdem());
        Assert.Equal(50, estrutura.Quantos);
        Invariante(estrutura);
    }

    /// <summary>
    /// O dominio INTEIRO em tamanho pequeno: todas as permutacoes de ate 7
    /// chaves, inseridas e depois removidas em outra permutacao.
    ///
    /// Sao 5.040 ordens de insercao, e para cada uma varias ordens de remocao.
    /// Sorteio quase nunca cai nos casos que quebram; aqui eles sao todos os
    /// casos.
    /// </summary>
    [Theory]
    [MemberData(nameof(Estruturas))]
    public void TodasAsOrdensDeAte7Chaves(string nome)
    {
        var sorteio = new Random(23);
        foreach (var insercao in Permutacoes([.. Enumerable.Range(0, 7)]))
        {
            var estrutura = Criar(nome);
            foreach (var chave in insercao)
            {
                estrutura.Inserir(chave, $"v{chave}");
                Invariante(estrutura);
            }
            Assert.Equal(Enumerable.Range(0, 7), estrutura.EmOrdem().Select(p => p.Chave));

            foreach (var chave in insercao.OrderBy(_ => sorteio.Next()))
            {
                Assert.True(estrutura.Remover(chave));
                Invariante(estrutura);
            }
            Assert.Equal(0, estrutura.Quantos);
        }
    }

    internal static IEnumerable<List<int>> Permutacoes(List<int> itens)
    {
        if (itens.Count <= 1) { yield return itens; yield break; }
        for (var i = 0; i < itens.Count; i++)
        {
            var resto = new List<int>(itens);
            resto.RemoveAt(i);
            foreach (var cauda in Permutacoes(resto))
                yield return [itens[i], .. cauda];
        }
    }
}
