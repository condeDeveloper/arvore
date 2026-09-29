using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// Dois juízes: o <c>SortedDictionary</c> do .NET e as próprias regras da árvore.
/// </summary>
/// <remarks>
/// <para>
/// O <c>SortedDictionary</c> é, ele mesmo, uma árvore rubro-negra — então a
/// comparação é contra uma implementação madura da <b>mesma</b> estrutura, e ela
/// é exata e ilimitada: qualquer sequência de operações pode ser comparada.
/// </para>
/// <para>
/// E há o segundo juiz, que é mais interessante: as <b>cinco regras</b>. Uma
/// árvore rubro-negra com um caso de remoção errado continua funcionando — ela
/// guarda, busca e devolve tudo certo, e só vai ficando torta. O
/// <c>SortedDictionary</c> nunca pegaria isso, porque as duas respondem igual.
/// </para>
/// <para>
/// Por isso as regras são conferidas depois de <b>cada</b> operação nos testes
/// pequenos. É o que transforma "a árvore funciona" em "a árvore é uma
/// rubro-negra".
/// </para>
/// </remarks>
public class JuizTest
{
    private static void ExigirSadia(ArvoreRubroNegra<int, string> arvore, string quando)
    {
        var problemas = arvore.Conferir();

        Assert.True(problemas.Count == 0,
            $"{quando}: {string.Join("; ", problemas)}");
    }

    [Fact(DisplayName = "as regras valem depois de CADA inserção e remoção")]
    public void AsRegrasValemSempre()
    {
        // Conferir depois de cada operação é caro e é o que pega o caso errado
        // no instante em que ele acontece -- e não num relatório de lentidão
        // seis meses depois.
        var sorteio = new Random(20260929);
        var arvore = new ArvoreRubroNegra<int, string>();
        var juiz = new SortedDictionary<int, string>();

        for (var i = 0; i < 3_000; i++)
        {
            var chave = sorteio.Next(0, 500);

            if (sorteio.Next(3) == 0 && juiz.Count > 0)
            {
                var removida = arvore.Remover(chave);

                Assert.Equal(juiz.Remove(chave), removida);

                ExigirSadia(arvore, $"depois de remover {chave} (passo {i})");
            }
            else
            {
                arvore.Por(chave, $"v{chave}");
                juiz[chave] = $"v{chave}";

                ExigirSadia(arvore, $"depois de inserir {chave} (passo {i})");
            }

            Assert.Equal(juiz.Count, arvore.Quantos);
        }

        Assert.Equal(juiz.ToList(), arvore.EmOrdem().ToList());
    }

    [Fact(DisplayName = "cem mil operações sorteadas contra o SortedDictionary")]
    public void CemMilOperacoes()
    {
        var sorteio = new Random(7);
        var arvore = new ArvoreRubroNegra<int, string>();
        var juiz = new SortedDictionary<int, string>();

        for (var i = 0; i < 100_000; i++)
        {
            var chave = sorteio.Next(0, 20_000);

            switch (sorteio.Next(10))
            {
                case 0:
                case 1:
                case 2:
                    Assert.Equal(juiz.Remove(chave), arvore.Remover(chave));
                    break;

                case 3:
                    Assert.Equal(juiz.ContainsKey(chave), arvore.Contem(chave));
                    break;

                case 4:
                {
                    var noJuiz = juiz.TryGetValue(chave, out var doJuiz);
                    var naArvore = arvore.TentarPegar(chave, out var daArvore);

                    Assert.Equal(noJuiz, naArvore);

                    if (noJuiz)
                    {
                        Assert.Equal(doJuiz, daArvore);
                    }

                    break;
                }

                default:
                    arvore.Por(chave, $"v{chave}-{i}");
                    juiz[chave] = $"v{chave}-{i}";
                    break;
            }

            Assert.Equal(juiz.Count, arvore.Quantos);
        }

        // No fim, tudo: a mesma ordem e os mesmos valores.
        Assert.Equal(juiz.ToList(), arvore.EmOrdem().ToList());

        ExigirSadia(arvore, "no fim de cem mil operações");
    }

    [Fact(DisplayName = "o caso que derruba a árvore ingênua: chaves ordenadas")]
    public void ChavesOrdenadas()
    {
        // É o motivo de a rubro-negra existir. Numa árvore de busca comum, isto
        // dá uma corrente de cem mil nós -- e dados que chegam ordenados são o
        // caso COMUM, não o raro.
        var arvore = new ArvoreRubroNegra<int, string>();

        for (var i = 0; i < 100_000; i++)
        {
            arvore.Por(i, $"v{i}");
        }

        ExigirSadia(arvore, "com cem mil chaves em ordem crescente");

        // Uma árvore ingênua teria altura 100.000. Esta tem de ter no máximo
        // 2·log₂(100.001) ≈ 34.
        Assert.InRange(arvore.Altura(), 17, 34);

        // E o mesmo em ordem decrescente, que é o outro lado do mesmo problema.
        var aoContrario = new ArvoreRubroNegra<int, string>();

        for (var i = 100_000; i > 0; i--)
        {
            aoContrario.Por(i, $"v{i}");
        }

        ExigirSadia(aoContrario, "com cem mil chaves em ordem decrescente");

        Assert.InRange(aoContrario.Altura(), 17, 34);
    }

    [Fact(DisplayName = "remover tudo, na ordem e ao contrário")]
    public void RemoverTudo()
    {
        // A remoção é a parte difícil, e remover TUDO exercita os seis casos
        // muitas vezes -- inclusive o que sobe até a raiz.
        foreach (var aoContrario in (bool[])[false, true])
        {
            var arvore = new ArvoreRubroNegra<int, string>();

            for (var i = 0; i < 2_000; i++)
            {
                arvore.Por(i, $"v{i}");
            }

            var ordem = Enumerable.Range(0, 2_000);

            foreach (var chave in aoContrario ? ordem.Reverse() : ordem)
            {
                Assert.True(arvore.Remover(chave));

                ExigirSadia(arvore, $"depois de remover {chave}");
            }

            Assert.Equal(0, arvore.Quantos);
            Assert.Empty(arvore.EmOrdem());
        }
    }

    [Fact(DisplayName = "remover numa ordem sorteada")]
    public void RemoverEmbaralhado()
    {
        var sorteio = new Random(11);

        for (var rodada = 0; rodada < 30; rodada++)
        {
            var arvore = new ArvoreRubroNegra<int, string>();
            var chaves = Enumerable.Range(0, 300).OrderBy(_ => sorteio.Next()).ToList();

            foreach (var chave in chaves)
            {
                arvore.Por(chave, $"v{chave}");
            }

            foreach (var chave in chaves.OrderBy(_ => sorteio.Next()))
            {
                Assert.True(arvore.Remover(chave));

                ExigirSadia(arvore, $"rodada {rodada}, depois de remover {chave}");
            }
        }
    }

    [Fact(DisplayName = "a altura fica dentro do teto de 2·log₂(n+1)")]
    public void AAlturaFicaNoTeto()
    {
        // As regras 3 e 4 juntas garantem que o caminho mais longo tem no máximo
        // o dobro do mais curto. O teto sai daí, e ele é o que transforma a
        // árvore numa estrutura com garantia -- e não numa que costuma ser boa.
        var sorteio = new Random(13);

        foreach (var quantos in (int[])[1, 10, 100, 1_000, 10_000, 100_000])
        {
            var arvore = new ArvoreRubroNegra<int, string>();

            for (var i = 0; i < quantos; i++)
            {
                arvore.Por(sorteio.Next(0, quantos * 3), "x");
            }

            var teto = 2 * (int)Math.Ceiling(Math.Log2(arvore.Quantos + 1));

            Assert.True(arvore.Altura() <= teto,
                $"com {arvore.Quantos} nós a altura é {arvore.Altura()} "
                + $"e o teto é {teto}");
        }
    }

    [Fact(DisplayName = "inserir a mesma chave troca o valor e não cresce")]
    public void MesmaChave()
    {
        var arvore = new ArvoreRubroNegra<int, string>();

        for (var i = 0; i < 1_000; i++)
        {
            arvore.Por(42, $"v{i}");

            Assert.Equal(1, arvore.Quantos);
        }

        Assert.Equal("v999", arvore[42]);

        ExigirSadia(arvore, "depois de mil inserções da mesma chave");
    }

    [Fact(DisplayName = "os casos de borda")]
    public void CasosDeBorda()
    {
        var arvore = new ArvoreRubroNegra<int, string>();

        // Vazia.
        Assert.Equal(0, arvore.Quantos);
        Assert.Empty(arvore.EmOrdem());
        Assert.False(arvore.Contem(1));
        Assert.False(arvore.Remover(1));
        Assert.Equal(0, arvore.Altura());

        ExigirSadia(arvore, "vazia");

        // Um nó só.
        arvore.Por(1, "um");

        Assert.Equal(1, arvore.Quantos);
        Assert.Equal(1, arvore.Altura());

        ExigirSadia(arvore, "com um nó");

        // E vazia de novo.
        Assert.True(arvore.Remover(1));

        Assert.Equal(0, arvore.Quantos);

        ExigirSadia(arvore, "vazia de novo");

        Assert.Throws<KeyNotFoundException>(() => _ = arvore[1]);
    }

    [Fact(DisplayName = "funciona com texto, que compara de outro jeito")]
    public void ComTexto()
    {
        // A árvore não sabe nada dos tipos: ela usa `CompareTo`. Trocar o tipo
        // de chave é o teste de que nada ficou preso a inteiros.
        var arvore = new ArvoreRubroNegra<string, int>();
        var juiz = new SortedDictionary<string, int>(StringComparer.Ordinal);

        var sorteio = new Random(17);

        for (var i = 0; i < 20_000; i++)
        {
            var chave = new string((char)('a' + sorteio.Next(26)), sorteio.Next(1, 6))
                + sorteio.Next(100);

            if (sorteio.Next(4) == 0)
            {
                Assert.Equal(juiz.Remove(chave), arvore.Remover(chave));
            }
            else
            {
                arvore.Por(chave, i);
                juiz[chave] = i;
            }
        }

        Assert.Equal(juiz.ToList(), arvore.EmOrdem().ToList());
    }
}
