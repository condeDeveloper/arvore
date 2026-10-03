using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// O juiz testado como qualquer outra coisa.
///
/// Ele e a referencia de todos os outros arquivos, entao um defeito aqui faria a
/// suite inteira concordar com a coisa errada. E ele e testado contra o
/// `SortedDictionary` da propria plataforma, que e uma rubro-negra escrita por
/// outra gente: duas implementacoes independentes precisam dar a mesma resposta.
/// </summary>
public class JuizTest
{
    [Fact]
    public void OJuizConcordaComOSortedDictionaryDaPlataforma()
    {
        var juiz = new Juiz<int, string>();
        var deles = new SortedDictionary<int, string>();
        var sorteio = new Random(3);

        for (var passo = 0; passo < 5000; passo++)
        {
            var chave = sorteio.Next(0, 300);
            var sorte = sorteio.NextDouble();

            if (sorte < 0.5)
            {
                var valor = $"v{passo}";
                var novoNoJuiz = juiz.Inserir(chave, valor);
                var novoNeles = !deles.ContainsKey(chave);
                deles[chave] = valor;
                Assert.Equal(novoNeles, novoNoJuiz);
            }
            else if (sorte < 0.8)
            {
                Assert.Equal(deles.Remove(chave), juiz.Remover(chave));
            }
            else
            {
                var achouNeles = deles.TryGetValue(chave, out var esperado);
                Assert.Equal(achouNeles, juiz.Achar(chave, out var obtido));
                if (achouNeles) Assert.Equal(esperado, obtido);
            }

            Assert.Equal(deles.Count, juiz.Quantos);
            Assert.Equal(deles.Keys, juiz.Chaves());
        }
    }

    [Fact]
    public void OJuizVazioRespondeComoVazio()
    {
        var juiz = new Juiz<int, string>();
        Assert.Equal(0, juiz.Quantos);
        Assert.Empty(juiz.EmOrdem());
        Assert.False(juiz.Achar(1, out _));
        Assert.False(juiz.Remover(1));
        Assert.Equal(-1, juiz.Altura);
    }

    /// <summary>
    /// A "altura" do juiz e a de uma busca binaria, que e o PISO de qualquer
    /// estrutura por comparacao.
    ///
    /// Nenhuma arvore de busca consegue ficar mais baixa que isso, porque cada
    /// comparacao responde um bit e sao precisos log2(n) bits para apontar um
    /// entre n elementos. E a referencia contra a qual as outras alturas sao
    /// lidas.
    /// </summary>
    [Fact]
    public void AAlturaDoJuizEhOPisoDeQualquerEstrutura()
    {
        foreach (var quantas in new[] { 1, 2, 7, 15, 16, 1000, 100_000 })
        {
            var juiz = new Juiz<int, string>();
            for (var i = 0; i < quantas; i++) juiz.Inserir(i, "x");
            Assert.Equal((int)Math.Floor(Math.Log2(quantas)), juiz.Altura);
        }

        // E nenhuma das estruturas fica abaixo dele.
        var chaves = Sequencias.Sorteadas(5000, semente: 7);
        var piso = (int)Math.Floor(Math.Log2(5000));
        foreach (var nome in new[] { "avl", "rubro", "treap", "pulo" })
        {
            var estrutura = ContratoTest.Criar(nome);
            foreach (var chave in chaves) estrutura.Inserir(chave, "x");
            Assert.True(estrutura.Altura >= piso, $"{nome} ficou com altura {estrutura.Altura}, abaixo do piso {piso}");
        }
    }

    /// <summary>
    /// O juiz nao duplica chave e mantem a ordem, que e o unico contrato que ele
    /// precisa cumprir para servir de referencia.
    /// </summary>
    [Fact]
    public void OJuizNaoDuplicaEMantemAOrdem()
    {
        var juiz = new Juiz<int, string>();
        var sorteio = new Random(11);
        for (var i = 0; i < 2000; i++) juiz.Inserir(sorteio.Next(0, 500), "x");

        var chaves = juiz.Chaves();
        Assert.Equal(chaves.OrderBy(x => x), chaves);
        Assert.Equal(chaves.Count, chaves.Distinct().Count());
        Assert.Equal(chaves.Count, juiz.Quantos);
    }
}
