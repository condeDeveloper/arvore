using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A arvore sem balanceamento, e a demonstracao de por que todas as outras
/// existem.
/// </summary>
public class BuscaTest
{
    /// <summary>
    /// Chaves em ordem crescente produzem uma LISTA ENCADEADA: altura n-1.
    ///
    /// Este e o caso mais comum do mundo real, e nao um caso de borda.
    /// Identificadores que crescem, registros importados de um arquivo
    /// ordenado, carimbos de tempo. A arvore responde tudo certo e gasta mais
    /// memoria que um vetor para fazer pior que ele.
    /// </summary>
    [Fact]
    public void ChavesOrdenadasViramListaEncadeada()
    {
        var arvore = new Busca<int, string>();
        foreach (var chave in Sequencias.Crescentes(500)) arvore.Inserir(chave, "x");

        Assert.Equal(499, arvore.Altura);
        Assert.Equal(500, arvore.Quantos);
        // E as respostas continuam todas certas, que e o que torna o defeito
        // dificil de notar.
        Assert.Equal(Enumerable.Range(0, 500), arvore.EmOrdem().Select(p => p.Chave));

        // Buscar a ultima chave custa uma comparacao por elemento.
        arvore.Achar(499, out _);
        Assert.Equal(500, arvore.ComparacoesDaUltima);
    }

    [Fact]
    public void ChavesDecrescentesTambemDegeneram()
    {
        var arvore = new Busca<int, string>();
        foreach (var chave in Sequencias.Decrescentes(300)) arvore.Inserir(chave, "x");
        Assert.Equal(299, arvore.Altura);
    }

    /// <summary>
    /// O zigue-zague degenera por um caminho DIFERENTE do crescente: cada chave
    /// nova vai para o lado oposto da anterior.
    ///
    /// Vale testar os dois porque uma correcao que so olha o caso crescente
    /// continua quebrada aqui.
    /// </summary>
    [Fact]
    public void OZigueZagueTambemDegenera()
    {
        var arvore = new Busca<int, string>();
        foreach (var chave in Sequencias.ZigueZague(200)) arvore.Inserir(chave, "x");
        Assert.Equal(199, arvore.Altura);
    }

    /// <summary>
    /// Com chaves SORTEADAS ela funciona bem, e e por isso que o defeito
    /// sobrevive: um teste feito so com sorteio nao mostra nada.
    ///
    /// A altura esperada de uma arvore montada com chaves em ordem aleatoria e
    /// 4,31 ln n, que e teorema (Devroye, 1986). Com mil chaves isso da perto de
    /// trinta, contra os 999 do caso ordenado.
    /// </summary>
    [Fact]
    public void ComChavesSorteadasElaFuncionaBem()
    {
        var arvore = new Busca<int, string>();
        foreach (var chave in Sequencias.Sorteadas(1000, semente: 5)) arvore.Inserir(chave, "x");

        var esperada = 4.31 * Math.Log(1000);
        Assert.InRange(arvore.Altura, 10, (int)(esperada * 1.5));
        // E o caminho medio fica perto de 2 ln n, que e o custo tipico de busca.
        Assert.InRange(arvore.CaminhoMedio(), 1.0, 2.5 * Math.Log(1000));
    }

    /// <summary>
    /// A degeneracao de Hibbard, de 1962: remover SEMPRE pelo sucessor piora a
    /// arvore com o tempo.
    ///
    /// Eu ia escrever isso como fato e a medida me obrigou a ser preciso. O
    /// efeito e real e so aparece no regime QUADRATICO: ele precisa da ordem de
    /// n² atualizacoes para se manifestar. Com 64 chaves e duzentos mil ciclos
    /// o comprimento medio de caminho nao muda nada; com 512 chaves e treze
    /// milhoes ele sobe mais de vinte por cento.
    ///
    /// Isso tambem explica por que o fenomeno e tao facil de nao ver: na escala
    /// em que se costuma testar, ele simplesmente nao acontece.
    /// </summary>
    [Fact]
    public void ARemocaoPeloSucessorSoDesequilibraNoRegimeQuadratico()
    {
        double Rodar(int n, int ciclos)
        {
            var arvore = new Busca<int, string>();
            var montadas = 0;
            double inicial = 0;
            foreach (var op in Sequencias.Hibbard(n, ciclos, semente: 29))
            {
                if (op > 0) arvore.Inserir(op, "x"); else arvore.Remover(-op);
                if (++montadas == n) inicial = arvore.CaminhoMedio();
            }
            Assert.Equal(n, arvore.Quantos);
            return arvore.CaminhoMedio() - inicial;
        }

        // Poucos ciclos para o tamanho: nada acontece.
        var curto = Rodar(256, 256 * 10);
        Assert.InRange(curto, -1.0, 1.0);

        // Da ordem de n² ciclos: o caminho medio cresce de verdade.
        var longo = Rodar(512, 50 * 512 * 512);
        Assert.True(longo > 1.5, $"o caminho medio subiu so {longo:F2}");
        Assert.True(longo > curto);
    }

    /// <summary>
    /// Os tres casos de remocao: folha, um filho e dois filhos. O terceiro e o
    /// unico interessante e o unico que costuma estar errado.
    /// </summary>
    [Fact]
    public void OsTresCasosDeRemocao()
    {
        var arvore = new Busca<int, string>();
        foreach (var chave in new[] { 50, 30, 70, 20, 40, 60, 80, 35, 45 })
            arvore.Inserir(chave, $"v{chave}");

        // Folha.
        Assert.True(arvore.Remover(20));
        Assert.Equal(new[] { 30, 35, 40, 45, 50, 60, 70, 80 }, arvore.EmOrdem().Select(p => p.Chave));

        // Um filho so: 60 nao existe mais depois, 70 fica com 80.
        Assert.True(arvore.Remover(60));
        Assert.Equal(new[] { 30, 35, 40, 45, 50, 70, 80 }, arvore.EmOrdem().Select(p => p.Chave));

        // Dois filhos: 40 tem 35 e 45.
        Assert.True(arvore.Remover(40));
        Assert.Equal(new[] { 30, 35, 45, 50, 70, 80 }, arvore.EmOrdem().Select(p => p.Chave));

        // E a raiz com dois filhos.
        Assert.True(arvore.Remover(50));
        Assert.Equal(new[] { 30, 35, 45, 70, 80 }, arvore.EmOrdem().Select(p => p.Chave));
        Assert.Equal(5, arvore.Quantos);
    }

    [Fact]
    public void ACarteiraDeComparacoesEhContada()
    {
        var arvore = new Busca<int, string>();
        for (var i = 0; i < 100; i++) arvore.Inserir(i, "x");
        var antes = arvore.ComparacoesNoTotal;

        arvore.Achar(99, out _);
        Assert.Equal(100, arvore.ComparacoesDaUltima);
        Assert.Equal(antes + 100, arvore.ComparacoesNoTotal);

        arvore.Achar(0, out _);
        Assert.Equal(1, arvore.ComparacoesDaUltima);
    }
}
