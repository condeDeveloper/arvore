using System.Diagnostics;
using Conde.Arvore;

namespace Conde.Arvore.Ferramentas;

/// <summary>
/// Mede o que a árvore promete: a altura e o número de rotações.
/// </summary>
/// <remarks>
/// <para>
/// Uma rubro-negra faz três promessas, e as três são números:
/// </para>
/// <list type="number">
///   <item><description>a altura fica em <c>O(log n)</c>, com teto de 2·log₂(n+1);</description></item>
///   <item><description>uma inserção faz no máximo <b>duas</b> rotações;</description></item>
///   <item><description>uma remoção faz no máximo <b>três</b>.</description></item>
/// </list>
/// <para>
/// As duas últimas são o que a separam de uma AVL, que mantém um equilíbrio
/// melhor e paga com <c>O(log n)</c> rotações por remoção. Numa estrutura que
/// muda muito, a rubro-negra ganha; numa que só consulta, a AVL.
/// </para>
/// </remarks>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var comando = argumentos.Length > 0 ? argumentos[0] : "tudo";

        if (comando is "tudo" or "altura")
        {
            Altura();
        }

        if (comando is "tudo" or "rotacoes")
        {
            Console.WriteLine();
            Rotacoes();
        }

        if (comando is "tudo" or "contra")
        {
            Console.WriteLine();
            ContraODotnet();
        }

        return 0;
    }

    private static void Altura()
    {
        Console.WriteLine("a altura, contra o teto teórico de 2·log₂(n+1)");
        Console.WriteLine();
        Console.WriteLine("nós".PadLeft(10) + "ordenadas".PadLeft(12)
            + "sorteadas".PadLeft(12) + "teto".PadLeft(8)
            + "ideal".PadLeft(8) + "  (ordenadas numa árvore sem balanço)");
        Console.WriteLine(new string('-', 74));

        var sorteio = new Random(20260929);

        foreach (var quantos in (int[])[10, 100, 1_000, 10_000, 100_000, 1_000_000])
        {
            var emOrdem = new ArvoreRubroNegra<int, int>();

            for (var i = 0; i < quantos; i++)
            {
                emOrdem.Por(i, i);
            }

            var embaralhada = new ArvoreRubroNegra<int, int>();

            foreach (var chave in Enumerable.Range(0, quantos).OrderBy(_ => sorteio.Next()))
            {
                embaralhada.Por(chave, chave);
            }

            var teto = 2 * (int)Math.Ceiling(Math.Log2(quantos + 1));
            var ideal = (int)Math.Ceiling(Math.Log2(quantos + 1));

            Console.WriteLine(
                quantos.ToString().PadLeft(10)
                + emOrdem.Altura().ToString().PadLeft(12)
                + embaralhada.Altura().ToString().PadLeft(12)
                + teto.ToString().PadLeft(8)
                + ideal.ToString().PadLeft(8)
                + $"   {quantos}");
        }

        Console.WriteLine();
        Console.WriteLine("A última coluna é o que uma árvore de busca SEM balanceamento");
        Console.WriteLine("teria com chaves ordenadas: uma corrente, e a busca custando n.");
    }

    private static void Rotacoes()
    {
        Console.WriteLine("rotações por operação");
        Console.WriteLine();
        Console.WriteLine("operações".PadLeft(12) + "inserções".PadLeft(12)
            + "por inserção".PadLeft(14) + "remoções".PadLeft(12)
            + "por remoção".PadLeft(14));
        Console.WriteLine(new string('-', 66));

        var sorteio = new Random(7);

        foreach (var quantas in (int[])[1_000, 10_000, 100_000, 1_000_000])
        {
            var arvore = new ArvoreRubroNegra<int, int>();

            var insercoes = 0;
            var remocoes = 0;

            long girosDeInsercao = 0;
            long girosDeRemocao = 0;

            for (var i = 0; i < quantas; i++)
            {
                var chave = sorteio.Next(0, quantas / 2 + 1);

                var antes = arvore.Rotacoes;

                if (sorteio.Next(3) == 0)
                {
                    arvore.Remover(chave);

                    remocoes++;
                    girosDeRemocao += arvore.Rotacoes - antes;
                }
                else
                {
                    arvore.Por(chave, i);

                    insercoes++;
                    girosDeInsercao += arvore.Rotacoes - antes;
                }
            }

            Console.WriteLine(
                quantas.ToString().PadLeft(12)
                + insercoes.ToString().PadLeft(12)
                + $"{(double)girosDeInsercao / insercoes:F3}".PadLeft(14)
                + remocoes.ToString().PadLeft(12)
                + $"{(double)girosDeRemocao / Math.Max(1, remocoes):F3}".PadLeft(14));
        }

        Console.WriteLine();
        Console.WriteLine("A promessa é no máximo 2 por inserção e 3 por remoção, e o que");
        Console.WriteLine("se mede é bem menos: a maioria das operações não rotaciona nada.");
        Console.WriteLine("É isso que a separa de uma AVL, que paga O(log n) por remoção.");
    }

    private static void ContraODotnet()
    {
        Console.WriteLine("contra o SortedDictionary, que é a mesma estrutura");
        Console.WriteLine();
        Console.WriteLine("operação".PadRight(20) + "nós".PadLeft(10)
            + "meu".PadLeft(12) + ".NET".PadLeft(12) + "razão".PadLeft(10));
        Console.WriteLine(new string('-', 64));

        foreach (var quantos in (int[])[10_000, 100_000, 1_000_000])
        {
            var chaves = Enumerable.Range(0, quantos)
                .OrderBy(_ => Guid.NewGuid())
                .ToArray();

            var meu = new ArvoreRubroNegra<int, int>();
            var dele = new SortedDictionary<int, int>();

            var meuInserir = Cronometrar(() =>
            {
                foreach (var chave in chaves)
                {
                    meu.Por(chave, chave);
                }
            });

            var deleInserir = Cronometrar(() =>
            {
                foreach (var chave in chaves)
                {
                    dele[chave] = chave;
                }
            });

            Linha("inserir", quantos, meuInserir, deleInserir);

            var meuBuscar = Cronometrar(() =>
            {
                foreach (var chave in chaves)
                {
                    meu.TentarPegar(chave, out _);
                }
            });

            var deleBuscar = Cronometrar(() =>
            {
                foreach (var chave in chaves)
                {
                    dele.TryGetValue(chave, out _);
                }
            });

            Linha("buscar", quantos, meuBuscar, deleBuscar);

            var meuPercorrer = Cronometrar(() =>
            {
                foreach (var _ in meu.EmOrdem())
                {
                }
            });

            var delePercorrer = Cronometrar(() =>
            {
                foreach (var _ in dele)
                {
                }
            });

            Linha("percorrer em ordem", quantos, meuPercorrer, delePercorrer);
        }

        Console.WriteLine();
        Console.WriteLine("Não é para ganhar: o SortedDictionary é a mesma estrutura, escrita");
        Console.WriteLine("por quem faz isso profissionalmente. Uma razão de duas ou três");
        Console.WriteLine("vezes quer dizer que o algoritmo está certo e falta o acabamento.");
    }

    private static void Linha(string nome, int quantos, double meu, double dele)
    {
        Console.WriteLine(nome.PadRight(20)
            + quantos.ToString().PadLeft(10)
            + $"{meu:F1} ms".PadLeft(12)
            + $"{dele:F1} ms".PadLeft(12)
            + $"{meu / Math.Max(0.001, dele):F1}x".PadLeft(10));
    }

    private static double Cronometrar(Action acao)
    {
        var relogio = Stopwatch.StartNew();

        acao();

        return relogio.Elapsed.TotalMilliseconds;
    }
}
