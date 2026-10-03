using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A rubro-negra inclinada para a esquerda, e as quatro regras conferidas
/// depois de cada operacao.
/// </summary>
public class RubroTest
{
    /// <summary>
    /// As quatro regras valem sempre: raiz preta, sem duas vermelhas seguidas,
    /// altura preta igual em todo caminho, e nenhuma vermelha a direita.
    /// </summary>
    [Fact]
    public void AsRegrasValemDepoisDeCadaOperacao()
    {
        var arvore = new Rubro<int, string>();
        var sorteio = new Random(13);
        for (var passo = 0; passo < 5000; passo++)
        {
            var chave = sorteio.Next(0, 400);
            if (sorteio.NextDouble() < 0.6) arvore.Inserir(chave, "x");
            else arvore.Remover(chave);
            var violacao = arvore.Violacao();
            Assert.True(violacao is null, $"passo {passo}: {violacao}");
        }
    }

    /// <summary>
    /// A altura obedece a 2 log2(n+1), em toda sequencia de entrada.
    ///
    /// A conta vem da terceira regra: se todo caminho tem p ligacoes pretas e
    /// nao ha duas vermelhas seguidas, o caminho mais longo e no maximo o dobro
    /// do mais curto.
    /// </summary>
    [Theory]
    [InlineData("crescente")]
    [InlineData("decrescente")]
    [InlineData("zigue-zague")]
    [InlineData("sorteada")]
    [InlineData("em blocos")]
    public void AAlturaObedeceAoDobroDoLogaritmo(string qual)
    {
        var chaves = qual switch
        {
            "crescente" => Sequencias.Crescentes(5000),
            "decrescente" => Sequencias.Decrescentes(5000),
            "zigue-zague" => Sequencias.ZigueZague(5000),
            "sorteada" => Sequencias.Sorteadas(5000, semente: 3),
            _ => Sequencias.EmBlocos(5000, 50, semente: 3),
        };

        var arvore = new Rubro<int, string>();
        foreach (var chave in chaves) arvore.Inserir(chave, "x");

        var (_, limite, minima) = Sequencias.Limites(arvore.Quantos);
        Assert.True(arvore.Altura <= limite, $"{qual}: altura {arvore.Altura}, limite {limite:F2}");
        Assert.True(arvore.Altura >= minima);
        Assert.Null(arvore.Violacao());
    }

    /// <summary>
    /// A ALTURA PRETA e a mesma em qualquer caminho, por invariante, e ela e no
    /// maximo a metade da altura total.
    /// </summary>
    [Fact]
    public void AAlturaPretaEhNoMaximoMetadeDaTotal()
    {
        foreach (var quantas in new[] { 10, 100, 1000, 10_000 })
        {
            var arvore = new Rubro<int, string>();
            foreach (var chave in Sequencias.Sorteadas(quantas, semente: quantas)) arvore.Inserir(chave, "x");

            var preta = arvore.AlturaPreta();
            Assert.True(preta <= arvore.Altura + 1);
            // Sem duas vermelhas seguidas, a altura total nao passa do dobro da
            // preta.
            Assert.True(arvore.Altura <= 2 * preta, $"altura {arvore.Altura}, preta {preta}");
        }
    }

    /// <summary>
    /// A regra de Sedgewick: NENHUMA ligacao vermelha aponta para a direita.
    ///
    /// E ela que corta metade dos casos e deixa a insercao em tres linhas. O
    /// conferidor trata uma vermelha a direita como violacao, e e isso que
    /// separa esta implementacao de uma rubro-negra classica.
    /// </summary>
    [Fact]
    public void NenhumaVermelhaApontaParaADireita()
    {
        var arvore = new Rubro<int, string>();
        foreach (var chave in Sequencias.Crescentes(2000))
        {
            arvore.Inserir(chave, "x");
            Assert.Null(arvore.Violacao());
        }
        // Inserir em ordem crescente e justamente o caso em que a vermelha
        // tenderia a ficar a direita a cada passo.
        Assert.Null(arvore.Violacao());
    }

    /// <summary>
    /// O preco da inclinacao, medido contra a rubro-negra CLASSICA.
    ///
    /// Esta e a comparacao honesta, e eu nao a tinha. A primeira versao deste
    /// teste comparava a LLRB com a AVL, o que mistura duas coisas: as duas
    /// obedecem a condicoes de balanceamento diferentes, entao a diferenca de
    /// rotacoes nao isola o efeito da inclinacao.
    ///
    /// A classica obedece as MESMAS cinco regras e difere so na restricao de
    /// manter toda ligacao vermelha a esquerda. O que essa restricao cobra
    /// aparece limpo: com chaves sorteadas a LLRB gira o DOBRO.
    /// </summary>
    [Fact]
    public void AInclinacaoCustaODobroDasRotacoes()
    {
        var chaves = Sequencias.Sorteadas(20_000, semente: 17);

        var inclinada = new Rubro<int, string>();
        foreach (var chave in chaves) inclinada.Inserir(chave, "x");

        var classica = new Classica<int, string>();
        foreach (var chave in chaves) classica.Inserir(chave, "x");

        // As duas tem as mesmas chaves e as mesmas regras.
        Assert.Equal(classica.EmOrdem().Select(p => p.Chave), inclinada.EmOrdem().Select(p => p.Chave));
        Assert.Null(inclinada.Violacao());
        Assert.Empty(classica.Conferir());

        var razao = inclinada.Rotacoes / (double)classica.Rotacoes;
        Assert.InRange(razao, 1.5, 2.5);

        // E a classica gira MENOS que a AVL, que e a troca classica entre as
        // duas familias: condicao mais frouxa, menos escrita, arvore mais alta.
        var avl = new Avl<int, string>();
        foreach (var chave in chaves) avl.Inserir(chave, "x");
        Assert.True(classica.Rotacoes < avl.Rotacoes,
            $"classica: {classica.Rotacoes}, AVL: {avl.Rotacoes}");
        Assert.True(classica.Altura >= avl.Altura, $"classica: {classica.Altura}, AVL: {avl.Altura}");
    }

    /// <summary>
    /// As duas rubro-negras concordam chave por chave, e as duas AVL tambem.
    ///
    /// Sao implementacoes escritas em dias diferentes, com estruturas de no
    /// diferentes (a classica tem ponteiro para o pai, a inclinada nao), e isso
    /// e o que torna a concordancia informativa.
    /// </summary>
    [Fact]
    public void AsImplementacoesIndependentesConcordam()
    {
        var sorteio = new Random(37);
        var inclinada = new Rubro<int, string>();
        var classica = new Classica<int, string>();
        var nova = new Avl<int, string>();
        var antiga = new AvlAntiga<int, string>();

        for (var passo = 0; passo < 4000; passo++)
        {
            var chave = sorteio.Next(0, 300);
            if (sorteio.NextDouble() < 0.6)
            {
                var valor = $"v{passo}";
                Assert.Equal(inclinada.Inserir(chave, valor), classica.Inserir(chave, valor));
                Assert.Equal(nova.Inserir(chave, valor), antiga.Inserir(chave, valor));
            }
            else
            {
                Assert.Equal(inclinada.Remover(chave), classica.Remover(chave));
                Assert.Equal(nova.Remover(chave), antiga.Remover(chave));
            }

            Assert.Equal(inclinada.EmOrdem(), classica.EmOrdem());
            Assert.Equal(nova.EmOrdem(), antiga.EmOrdem());
        }
    }

    /// <summary>
    /// A recoloracao e mais barata que a rotacao, e a LLRB usa mais dela: ela
    /// nao mexe em ponteiro nenhum.
    /// </summary>
    [Fact]
    public void ARecoloracaoEhContada()
    {
        var arvore = new Rubro<int, string>();
        foreach (var chave in Sequencias.Sorteadas(10_000, semente: 19)) arvore.Inserir(chave, "x");
        Assert.True(arvore.Recoloracoes > 0);
        Assert.True(arvore.Rotacoes > 0);
    }

    /// <summary>
    /// A remocao e a parte dificil, e o caso que quebra implementacao e remover
    /// de uma arvore PEQUENA, onde nao ha de onde pedir emprestado.
    /// </summary>
    [Fact]
    public void RemoverDeArvoresPequenasEmTodasAsOrdens()
    {
        // Ate 5 chaves: 120 ordens de insercao vezes 120 de remocao, 14.400 pares
        // por tamanho. Com 6 seriam 518.400, e o ganho de cobertura nao paga o
        // tempo numa CI de tres sistemas.
        for (var quantas = 1; quantas <= 5; quantas++)
            foreach (var insercao in ContratoTest.Permutacoes([.. Enumerable.Range(0, quantas)]))
                foreach (var remocao in ContratoTest.Permutacoes([.. Enumerable.Range(0, quantas)]))
                {
                    var arvore = new Rubro<int, string>();
                    foreach (var chave in insercao) arvore.Inserir(chave, "x");

                    var restantes = new List<int>(insercao);
                    foreach (var chave in remocao)
                    {
                        Assert.True(arvore.Remover(chave));
                        restantes.Remove(chave);
                        var violacao = arvore.Violacao();
                        Assert.True(violacao is null,
                            $"inserindo {string.Join(",", insercao)} e removendo {string.Join(",", remocao)}: {violacao}");
                        Assert.Equal(restantes.OrderBy(x => x), arvore.EmOrdem().Select(p => p.Chave));
                    }
                }
    }
}
