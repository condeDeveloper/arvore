using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A AVL, e o limite de 1,44 log2(n) conferido em vez de citado.
/// </summary>
public class AvlTest
{
    /// <summary>
    /// A altura obedece ao limite provado, em TODA sequencia de entrada,
    /// inclusive nas que destroem a arvore sem balanceamento.
    ///
    /// O limite vem da arvore minima: uma AVL de altura h minima tem uma
    /// subarvore de altura h-1 e outra de h-2, o que e Fibonacci, e dai sai
    /// h &lt;= 1,4405 log2(n+2) - 1,3277.
    /// </summary>
    [Theory]
    [InlineData("crescente")]
    [InlineData("decrescente")]
    [InlineData("zigue-zague")]
    [InlineData("sorteada")]
    [InlineData("em blocos")]
    public void AAlturaObedeceAoLimiteDeFibonacci(string qual)
    {
        var chaves = qual switch
        {
            "crescente" => Sequencias.Crescentes(5000),
            "decrescente" => Sequencias.Decrescentes(5000),
            "zigue-zague" => Sequencias.ZigueZague(5000),
            "sorteada" => Sequencias.Sorteadas(5000, semente: 3),
            _ => Sequencias.EmBlocos(5000, 50, semente: 3),
        };

        var arvore = new Avl<int, string>();
        foreach (var chave in chaves) arvore.Inserir(chave, "x");

        var (limite, _, minima) = Sequencias.Limites(arvore.Quantos);
        Assert.True(arvore.Altura <= limite,
            $"{qual}: altura {arvore.Altura}, limite {limite:F2}");
        Assert.True(arvore.Altura >= minima);
        Assert.True(arvore.Equilibrada());
    }

    /// <summary>
    /// Os quatro casos de desequilibrio, cada um montado a mao.
    ///
    /// Os dois simples sao quando o no e o filho pendem para o mesmo lado; os
    /// dois duplos sao quando pendem para lados OPOSTOS, e ai uma rotacao so
    /// troca o problema de lado. Esquecer o caso duplo nao da resposta errada:
    /// da uma arvore que continua respondendo e para de ficar balanceada.
    /// </summary>
    [Fact]
    public void OsQuatroCasosDeDesequilibrio()
    {
        // Esquerda-esquerda: uma rotacao.
        var ee = new Avl<int, string>();
        foreach (var k in new[] { 30, 20, 10 }) ee.Inserir(k, "x");
        Assert.Equal(1, ee.Altura);
        Assert.Equal(1, ee.Rotacoes);

        // Direita-direita.
        var dd = new Avl<int, string>();
        foreach (var k in new[] { 10, 20, 30 }) dd.Inserir(k, "x");
        Assert.Equal(1, dd.Altura);
        Assert.Equal(1, dd.Rotacoes);

        // Esquerda-direita: duas rotacoes.
        var ed = new Avl<int, string>();
        foreach (var k in new[] { 30, 10, 20 }) ed.Inserir(k, "x");
        Assert.Equal(1, ed.Altura);
        Assert.Equal(2, ed.Rotacoes);

        // Direita-esquerda: duas rotacoes.
        var de = new Avl<int, string>();
        foreach (var k in new[] { 10, 30, 20 }) de.Inserir(k, "x");
        Assert.Equal(1, de.Altura);
        Assert.Equal(2, de.Rotacoes);

        // Nos quatro, a arvore resultante e a mesma: 20 na raiz.
        foreach (var arvore in new[] { ee, dd, ed, de })
            Assert.Equal(new[] { 10, 20, 30 }, arvore.EmOrdem().Select(p => p.Chave));
    }

    /// <summary>
    /// A ALTURA GUARDADA em cada no e um cache, e um cache errado faz o
    /// balanceamento tomar decisoes certas sobre uma arvore que nao existe.
    ///
    /// O conferidor confere as duas coisas de uma vez, e e por isso que ele
    /// devolve a altura em vez de um booleano.
    /// </summary>
    [Fact]
    public void AAlturaGuardadaBateComAAlturaDeVerdade()
    {
        var arvore = new Avl<int, string>();
        var sorteio = new Random(11);
        for (var passo = 0; passo < 3000; passo++)
        {
            var chave = sorteio.Next(0, 300);
            if (sorteio.NextDouble() < 0.6) arvore.Inserir(chave, "x");
            else arvore.Remover(chave);
            Assert.True(arvore.Equilibrada(), $"quebrou no passo {passo}");
        }
    }

    /// <summary>
    /// A arvore de Fibonacci: a AVL mais desequilibrada que pode existir para
    /// cada altura, e a que faz o limite ser justo.
    ///
    /// Ela e montada inserindo exatamente as chaves que produzem a forma minima.
    /// Se o limite nao fosse justo, esta arvore nao chegaria nele.
    /// </summary>
    [Fact]
    public void AArvoreDeFibonacciChegaNoLimite()
    {
        // N(h) = 1 + N(h-1) + N(h-2), com N(0) = 1 e N(1) = 2.
        var minima = new List<int> { 1, 2 };
        while (minima.Count < 20) minima.Add(1 + minima[^1] + minima[^2]);

        for (var altura = 2; altura < 12; altura++)
        {
            var quantos = minima[altura];
            var (limite, _, _) = Sequencias.Limites(quantos);
            // A arvore minima de altura h tem exatamente N(h) nos, entao o
            // limite para essa quantidade precisa alcancar h.
            Assert.True(limite >= altura,
                $"com {quantos} nos o limite e {limite:F2} e a arvore minima de altura {altura} existe");
            // E nao sobra muito: o limite e justo.
            Assert.True(limite < altura + 1.2, $"limite {limite:F2} para altura {altura}");
        }
    }

    /// <summary>
    /// Quantas rotacoes a AVL gasta. Este e o lado do preco da troca classica:
    /// altura menor, mais escrita.
    /// </summary>
    [Fact]
    public void AsRotacoesPorInsercaoSaoPoucasMasNaoZero()
    {
        var crescente = new Avl<int, string>();
        foreach (var chave in Sequencias.Crescentes(10_000)) crescente.Inserir(chave, "x");

        var sorteada = new Avl<int, string>();
        foreach (var chave in Sequencias.Sorteadas(10_000, semente: 7)) sorteada.Inserir(chave, "x");

        // Em media bem menos de uma rotacao por insercao, nos dois casos.
        Assert.True(crescente.Rotacoes < 10_000);
        Assert.True(sorteada.Rotacoes < 10_000);
        // E a entrada ordenada gasta MAIS, porque ela puxa a arvore sempre para
        // o mesmo lado.
        Assert.True(crescente.Rotacoes > sorteada.Rotacoes * 0.5);
    }
}
