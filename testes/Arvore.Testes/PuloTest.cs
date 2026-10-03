using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A lista de pulos: as moedas, os niveis e o remendo de ponteiros.
/// </summary>
public class PuloTest
{
    /// <summary>
    /// A consistencia vale depois de cada operacao: todo nivel ordenado e
    /// subconjunto do nivel de baixo.
    ///
    /// A segunda parte e a que pega o defeito de verdade. Um remendo que esquece
    /// um nivel deixa a lista respondendo certo pelo nivel zero, que tem todo
    /// mundo, com um atalho apontando para um no removido. A busca passa a pular
    /// elementos, e so as vezes.
    /// </summary>
    [Fact]
    public void AConsistenciaValeDepoisDeCadaOperacao()
    {
        var pulo = new Pulo<int, string>(semente: 3);
        var sorteio = new Random(5);
        for (var passo = 0; passo < 5000; passo++)
        {
            var chave = sorteio.Next(0, 400);
            if (sorteio.NextDouble() < 0.6) pulo.Inserir(chave, "x");
            else pulo.Remover(chave);
            Assert.True(pulo.Consistente(), $"inconsistente no passo {passo}");
        }
    }

    /// <summary>
    /// A distribuicao dos niveis e geometrica: cada nivel tem perto de metade
    /// dos elementos do nivel de baixo.
    ///
    /// E dessa proporcao que vem o tempo logaritmico, e e a unica coisa que
    /// precisa estar certa na moeda. Uma moeda viciada nao quebra a estrutura:
    /// quebra o desempenho, em silencio.
    /// </summary>
    [Fact]
    public void OsNiveisSeguemAMoeda()
    {
        var pulo = new Pulo<int, string>(semente: 7);
        const int quantas = 100_000;
        for (var chave = 0; chave < quantas; chave++) pulo.Inserir(chave, "x");

        var porNivel = pulo.PorNivel();
        Assert.Equal(quantas, porNivel[0]);
        for (var nivel = 1; nivel < porNivel.Count && porNivel[nivel - 1] > 1000; nivel++)
        {
            var razao = porNivel[nivel] / (double)porNivel[nivel - 1];
            Assert.InRange(razao, 0.45, 0.55);
        }
        // E a quantidade de niveis fica perto de log2(n), que com cem mil e 17.
        Assert.InRange(pulo.Niveis, 13, 24);
    }

    /// <summary>
    /// A ordem de chegada nao muda nada, porque a altura de cada elemento vem do
    /// sorteio e nao da estrutura. Nao existe sequencia adversaria.
    /// </summary>
    [Theory]
    [InlineData("crescente")]
    [InlineData("decrescente")]
    [InlineData("zigue-zague")]
    [InlineData("sorteada")]
    public void AOrdemDeChegadaNaoMudaNada(string qual)
    {
        var chaves = qual switch
        {
            "crescente" => Sequencias.Crescentes(10_000),
            "decrescente" => Sequencias.Decrescentes(10_000),
            "zigue-zague" => Sequencias.ZigueZague(10_000),
            _ => Sequencias.Sorteadas(10_000, semente: 3),
        };

        var pulo = new Pulo<int, string>(semente: 11);
        foreach (var chave in chaves) pulo.Inserir(chave, "x");

        Assert.InRange(pulo.Niveis, 10, 22);
        Assert.True(pulo.Consistente());
        Assert.Equal(10_000, pulo.Quantos);
    }

    /// <summary>
    /// Os niveis que esvaziam DESAPARECEM. Sem isso a busca comecaria num nivel
    /// sem nada e pagaria por nada.
    /// </summary>
    [Fact]
    public void OsNiveisVaziosSomem()
    {
        var pulo = new Pulo<int, string>(semente: 13);
        for (var chave = 0; chave < 10_000; chave++) pulo.Inserir(chave, "x");
        var altos = pulo.Niveis;
        Assert.True(altos > 5);

        for (var chave = 0; chave < 9_999; chave++) pulo.Remover(chave);
        Assert.Equal(1, pulo.Quantos);
        Assert.True(pulo.Niveis < altos, $"ainda com {pulo.Niveis} niveis para um elemento");
        Assert.True(pulo.Consistente());

        pulo.Remover(9_999);
        Assert.Equal(1, pulo.Niveis);
        Assert.Equal(-1, pulo.Altura);
    }

    /// <summary>
    /// A busca custa perto de log2(n) comparacoes, que e o que a estrutura
    /// promete. O teste compara com a lista encadeada simples, que seria n/2.
    /// </summary>
    [Fact]
    public void ABuscaCustaLogaritmo()
    {
        var pulo = new Pulo<int, string>(semente: 17);
        const int quantas = 50_000;
        for (var chave = 0; chave < quantas; chave++) pulo.Inserir(chave, "x");

        long total = 0;
        var sorteio = new Random(19);
        for (var i = 0; i < 1000; i++)
        {
            pulo.Achar(sorteio.Next(quantas), out _);
            total += pulo.ComparacoesDaUltima;
        }

        var media = total / 1000.0;
        var logaritmo = Math.Log2(quantas);
        // A teoria diz perto de 2 log2(n) comparacoes: uma por nivel descido
        // mais uma por passo lateral.
        Assert.InRange(media, logaritmo, 4 * logaritmo);
        // E muito abaixo da lista encadeada, que seria 25 mil.
        Assert.True(media < quantas / 100.0);
    }

    /// <summary>
    /// O teto de niveis e respeitado mesmo com muita insercao, e ele e grande o
    /// bastante: com 32 niveis cabem quatro bilhoes de elementos.
    /// </summary>
    [Fact]
    public void OTetoDeNiveisEhRespeitado()
    {
        var pulo = new Pulo<int, string>(semente: 23);
        for (var chave = 0; chave < 20_000; chave++) pulo.Inserir(chave, "x");
        Assert.True(pulo.Niveis <= Pulo<int, string>.TetoDeNiveis);
        Assert.Equal(32, Pulo<int, string>.TetoDeNiveis);
        // Com metade por nivel, 32 niveis cobrem 2^32 elementos.
        Assert.True(Math.Pow(2, Pulo<int, string>.TetoDeNiveis) > 4e9);
    }
}
