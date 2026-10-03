using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// As sequencias de entrada, e os limites teoricos contra os quais as alturas
/// sao lidas.
///
/// Este arquivo testa as proprias ferramentas de teste. Uma sequencia mal
/// construida faria os outros arquivos passarem ou falharem por culpa dela, e
/// nao da estrutura.
/// </summary>
public class SequenciasTest
{
    [Fact]
    public void AsSequenciasTemOFormatoQueDizemTer()
    {
        Assert.Equal(Enumerable.Range(0, 100), Sequencias.Crescentes(100));
        Assert.Equal(Enumerable.Range(0, 100).Reverse(), Sequencias.Decrescentes(100));

        var sorteadas = Sequencias.Sorteadas(1000, semente: 3);
        Assert.Equal(1000, sorteadas.Count);
        Assert.Equal(1000, sorteadas.Distinct().Count());
        // E elas nao estao em ordem, que e o ponto.
        Assert.NotEqual(sorteadas.OrderBy(x => x), sorteadas);
    }

    /// <summary>
    /// O zigue-zague alterna extremos, e por isso ele degenera a arvore sem
    /// balanceamento por um caminho diferente do crescente.
    /// </summary>
    [Fact]
    public void OZigueZagueAlternaExtremos()
    {
        var zigue = Sequencias.ZigueZague(10);
        Assert.Equal(new[] { 0, 9, 1, 8, 2, 7, 3, 6, 4, 5 }, zigue);
        Assert.Equal(10, zigue.Distinct().Count());

        // Em tamanho impar a do meio entra uma vez so.
        var impar = Sequencias.ZigueZague(7);
        Assert.Equal(7, impar.Count);
        Assert.Equal(7, impar.Distinct().Count());
    }

    /// <summary>
    /// Os blocos sao internamente ordenados e embaralhados entre si: e o formato
    /// de dados vindos de varios arquivos ordenados, concatenados.
    /// </summary>
    [Fact]
    public void OsBlocosSaoOrdenadosPorDentroEEmbaralhadosPorFora()
    {
        var chaves = Sequencias.EmBlocos(100, 10, semente: 5);
        Assert.Equal(100, chaves.Count);
        Assert.Equal(100, chaves.Distinct().Count());

        for (var inicio = 0; inicio < 100; inicio += 10)
        {
            var bloco = chaves.Skip(inicio).Take(10).ToList();
            Assert.Equal(bloco.OrderBy(x => x), bloco);
        }
        // E os blocos nao estao na ordem natural.
        Assert.NotEqual(Enumerable.Range(0, 100), chaves);
    }

    /// <summary>
    /// A sequencia de Hibbard alterna insercao e remocao, e so remove o que esta
    /// vivo. Uma sequencia que remove o que nao existe nao testaria nada.
    /// </summary>
    [Fact]
    public void AHibbardSoRemoveOQueEstaVivo()
    {
        var vivas = new HashSet<int>();
        var insercoes = 0;
        var remocoes = 0;
        foreach (var op in Sequencias.Hibbard(200, 2000, semente: 7))
        {
            if (op > 0)
            {
                Assert.True(vivas.Add(op), $"a chave {op} foi inserida duas vezes");
                insercoes++;
            }
            else
            {
                Assert.True(vivas.Remove(-op), $"removeu {-op}, que nao estava viva");
                remocoes++;
            }
        }
        Assert.Equal(2000, remocoes);
        Assert.Equal(2200, insercoes);
        // E a populacao fica PARADA em duzentas chaves, que e a hipotese do
        // experimento: a degeneracao de Hibbard e sobre uma arvore de tamanho
        // constante sofrendo atualizacoes, nao sobre uma que cresce.
        Assert.Equal(200, vivas.Count);
    }

    /// <summary>
    /// Os limites teoricos, conferidos nos casos conhecidos.
    ///
    /// Estes numeros nao sao estimativas. O da AVL vem da arvore minima de
    /// Fibonacci; o da rubro-negra vem de nao haver duas vermelhas seguidas com
    /// altura preta constante; e o piso vem da teoria da informacao.
    /// </summary>
    [Fact]
    public void OsLimitesSaoOsProvados()
    {
        var (avl, rubro, minima) = Sequencias.Limites(1_000_000);
        // 1,44 log2(10^6) e perto de 28; 2 log2(10^6) e perto de 40.
        Assert.InRange(avl, 27, 29);
        Assert.InRange(rubro, 39, 41);
        Assert.Equal(19, minima);

        // E o da AVL e sempre menor que o da rubro-negra, que e a diferenca
        // entre as duas estruturas dita em numero.
        foreach (var n in new[] { 10, 100, 10_000, 1_000_000 })
        {
            var (a, r, _) = Sequencias.Limites(n);
            Assert.True(a < r, $"com {n}: AVL {a:F2}, rubro {r:F2}");
        }

        Assert.Equal((0.0, 0.0, 0.0), Sequencias.Limites(0));
    }
}
