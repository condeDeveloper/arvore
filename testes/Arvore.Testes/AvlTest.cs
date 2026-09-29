using Xunit;

namespace Conde.Arvore.Testes;

/// <summary>
/// A AVL, contra o mesmo juiz — e a comparação entre as duas.
/// </summary>
/// <remarks>
/// A AVL está no projeto para que a frase "a AVL é mais equilibrada e a
/// rubro-negra rotaciona menos" venha com o número junto. Aqui estão os testes
/// que medem as duas metades dessa frase.
/// </remarks>
public class AvlTest
{
    private static void ExigirSadia<TValor>(ArvoreAvl<int, TValor> arvore, string quando)
    {
        var problemas = arvore.Conferir();

        Assert.True(problemas.Count == 0, $"{quando}: {string.Join("; ", problemas)}");
    }

    [Fact(DisplayName = "a regra da AVL vale depois de CADA operação")]
    public void ARegraValeSempre()
    {
        var sorteio = new Random(20260929);
        var arvore = new ArvoreAvl<int, string>();
        var juiz = new SortedDictionary<int, string>();

        for (var i = 0; i < 3_000; i++)
        {
            var chave = sorteio.Next(0, 500);

            if (sorteio.Next(3) == 0)
            {
                Assert.Equal(juiz.Remove(chave), arvore.Remover(chave));

                ExigirSadia(arvore, $"depois de remover {chave}");
            }
            else
            {
                arvore.Por(chave, $"v{chave}");
                juiz[chave] = $"v{chave}";

                ExigirSadia(arvore, $"depois de inserir {chave}");
            }

            Assert.Equal(juiz.Count, arvore.Quantos);
        }

        Assert.Equal(juiz.ToList(), arvore.EmOrdem().ToList());
    }

    [Fact(DisplayName = "cem mil operações sorteadas contra o SortedDictionary")]
    public void CemMilOperacoes()
    {
        var sorteio = new Random(7);
        var arvore = new ArvoreAvl<int, string>();
        var juiz = new SortedDictionary<int, string>();

        for (var i = 0; i < 100_000; i++)
        {
            var chave = sorteio.Next(0, 20_000);

            if (sorteio.Next(3) == 0)
            {
                Assert.Equal(juiz.Remove(chave), arvore.Remover(chave));
            }
            else
            {
                arvore.Por(chave, $"v{chave}-{i}");
                juiz[chave] = $"v{chave}-{i}";
            }

            Assert.Equal(juiz.Count, arvore.Quantos);
        }

        Assert.Equal(juiz.ToList(), arvore.EmOrdem().ToList());

        ExigirSadia(arvore, "no fim de cem mil operações");
    }

    [Fact(DisplayName = "a AVL fica MAIS BAIXA que a rubro-negra")]
    public void AAvlEhMaisBaixa()
    {
        // É a metade boa da troca, e dá para medir. A altura de uma AVL fica
        // em torno de 1,44·log₂(n); a de uma rubro-negra, até 2·log₂(n).
        foreach (var quantos in (int[])[1_000, 10_000, 100_000])
        {
            var avl = new ArvoreAvl<int, int>();
            var rubroNegra = new ArvoreRubroNegra<int, int>();

            for (var i = 0; i < quantos; i++)
            {
                avl.Por(i, i);
                rubroNegra.Por(i, i);
            }

            Assert.True(avl.Altura() < rubroNegra.Altura(),
                $"com {quantos} chaves ordenadas: AVL {avl.Altura()}, "
                + $"rubro-negra {rubroNegra.Altura()}");

            // E dentro do teto teórico de 1,44·log₂(n+2) − 0,33.
            var teto = (int)Math.Ceiling(1.4405 * Math.Log2(quantos + 2) - 0.3277);

            Assert.True(avl.Altura() <= teto,
                $"altura {avl.Altura()} passa do teto de {teto}");
        }
    }

    [Fact(DisplayName = "a diferença de rotações é no PIOR CASO, e não na média")]
    public void ADiferencaEhNoPiorCaso()
    {
        // Este teste começou dizendo outra coisa. Eu tinha escrito "a AVL
        // rotaciona mais que a rubro-negra ao remover", que é o que todo texto
        // sobre estruturas diz, e ele FALHOU: em 50 mil remoções, a AVL fez
        // 18.719 rotações e a rubro-negra 18.941. A rubro-negra rotacionou mais.
        //
        // A frase do livro não estava errada -- a minha leitura dela estava. O
        // O(log n) da AVL é PIOR CASO, e na média as duas gastam menos de meia
        // rotação por remoção. A diferença existe e está em outro lugar: no
        // máximo que UMA remoção pode custar.
        //
        // A rubro-negra tem teto de três, e ele vale sempre. A AVL não tem teto
        // constante nenhum.
        var sorteio = new Random(11);

        var avl = new ArvoreAvl<int, int>();
        var rubroNegra = new ArvoreRubroNegra<int, int>();

        const int quantos = 50_000;

        var chaves = Enumerable.Range(0, quantos).OrderBy(_ => sorteio.Next()).ToList();

        foreach (var chave in chaves)
        {
            avl.Por(chave, chave);
            rubroNegra.Por(chave, chave);
        }

        long avlTotal = 0;
        long rubroNegraTotal = 0;

        long avlPior = 0;
        long rubroNegraPior = 0;

        foreach (var chave in chaves.OrderBy(_ => sorteio.Next()))
        {
            var antesAvl = avl.Rotacoes;
            var antesRn = rubroNegra.Rotacoes;

            avl.Remover(chave);
            rubroNegra.Remover(chave);

            var nestaAvl = avl.Rotacoes - antesAvl;
            var nestaRn = rubroNegra.Rotacoes - antesRn;

            avlTotal += nestaAvl;
            rubroNegraTotal += nestaRn;

            avlPior = Math.Max(avlPior, nestaAvl);
            rubroNegraPior = Math.Max(rubroNegraPior, nestaRn);
        }

        // A promessa da rubro-negra: NENHUMA remoção passa de três rotações.
        Assert.True(rubroNegraPior <= 3,
            $"uma remoção da rubro-negra fez {rubroNegraPior} rotações, "
            + "e o teto do algoritmo é três");

        // A AVL passa, e é isso que a frase do livro quer dizer.
        Assert.True(avlPior > 3,
            $"a pior remoção da AVL fez {avlPior} rotações; "
            + "esperava mais de três, que é o teto da rubro-negra");

        // E na média as duas empatam -- que foi a surpresa.
        var mediaAvl = (double)avlTotal / quantos;
        var mediaRn = (double)rubroNegraTotal / quantos;

        Assert.InRange(mediaAvl, 0.1, 1.0);
        Assert.InRange(mediaRn, 0.1, 1.0);

        Assert.True(Math.Abs(mediaAvl - mediaRn) < 0.3,
            $"as médias deviam ficar perto: AVL {mediaAvl:F3}, "
            + $"rubro-negra {mediaRn:F3}");
    }

    [Fact(DisplayName = "as duas dão exatamente a mesma sequência ordenada")]
    public void AsDuasConcordam()
    {
        // O balanceamento é invisível de fora: as duas árvores, com as mesmas
        // chaves, devolvem exatamente a mesma coisa. É essa a propriedade que
        // permite trocar uma pela outra sem ninguém perceber -- e a única
        // diferença que sobra é o custo.
        var sorteio = new Random(13);

        var avl = new ArvoreAvl<int, string>();
        var rubroNegra = new ArvoreRubroNegra<int, string>();

        for (var i = 0; i < 50_000; i++)
        {
            var chave = sorteio.Next(0, 10_000);

            if (sorteio.Next(3) == 0)
            {
                Assert.Equal(rubroNegra.Remover(chave), avl.Remover(chave));
            }
            else
            {
                avl.Por(chave, $"v{i}");
                rubroNegra.Por(chave, $"v{i}");
            }

            Assert.Equal(rubroNegra.Quantos, avl.Quantos);
        }

        Assert.Equal(rubroNegra.EmOrdem().ToList(), avl.EmOrdem().ToList());
    }

    [Fact(DisplayName = "chaves ordenadas, que é o caso que motiva tudo")]
    public void ChavesOrdenadas()
    {
        var arvore = new ArvoreAvl<int, int>();

        for (var i = 0; i < 100_000; i++)
        {
            arvore.Por(i, i);
        }

        ExigirSadia(arvore, "com cem mil chaves em ordem");

        Assert.InRange(arvore.Altura(), 17, 25);
    }

    [Fact(DisplayName = "os casos de borda")]
    public void CasosDeBorda()
    {
        var arvore = new ArvoreAvl<int, string>();

        Assert.Equal(0, arvore.Quantos);
        Assert.Empty(arvore.EmOrdem());
        Assert.False(arvore.Remover(1));

        ExigirSadia(arvore, "vazia");

        arvore.Por(1, "um");
        arvore.Por(1, "outro");

        Assert.Equal(1, arvore.Quantos);
        Assert.True(arvore.TentarPegar(1, out var valor));
        Assert.Equal("outro", valor);

        Assert.True(arvore.Remover(1));
        Assert.Equal(0, arvore.Quantos);

        ExigirSadia(arvore, "vazia de novo");
    }
}
