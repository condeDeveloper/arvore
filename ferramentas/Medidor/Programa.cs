using Conde.Arvore;

namespace Conde.Arvore.Medidor;

/// <summary>
/// As medidas. O que se mede e ALTURA e CONTAGEM DE COMPARACAO, que sao
/// propriedades do algoritmo e dao o mesmo numero em qualquer maquina.
///
/// Tempo de relogio nao entra. Ele depende de cache, de frequencia e de quem
/// mais esta rodando, e numa comparacao entre estruturas de arvore ele atrapalha
/// mais do que informa.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "altura": Alturas(); break;
            case "busca": Buscas(); break;
            case "escrita": Escritas(); break;
            case "hibbard": Hibbard(); break;
            case "niveis": Niveis(); break;
            case "tudo": Alturas(); Buscas(); Escritas(); Hibbard(); Niveis(); break;
            default:
                Console.Error.WriteLine("medidas: altura, busca, escrita, hibbard, niveis, tudo");
                return 1;
        }
        return 0;
    }

    private const int Quantas = 100_000;

    private static (string Nome, List<int> Chaves)[] Entradas() =>
    [
        ("sorteada", Sequencias.Sorteadas(Quantas, 3)),
        ("crescente", Sequencias.Crescentes(Quantas)),
        ("decrescente", Sequencias.Decrescentes(Quantas)),
        ("zigue-zague", Sequencias.ZigueZague(Quantas)),
        ("em blocos", Sequencias.EmBlocos(Quantas, 100, 3)),
    ];

    private static IDicionario<int, int> Criar(string nome) => nome switch
    {
        "busca" => new Busca<int, int>(),
        "avl" => new Avl<int, int>(),
        "rubro" => new Rubro<int, int>(),
        "treap" => new Treap<int, int>(semente: 101),
        "pulo" => new Pulo<int, int>(semente: 101),
        _ => throw new ArgumentOutOfRangeException(nameof(nome)),
    };

    private static readonly string[] Nomes = ["busca", "avl", "rubro", "treap", "pulo"];

    /// <summary>
    /// A tabela principal: altura de cada estrutura em cada sequencia de
    /// entrada.
    ///
    /// E a tabela que justifica o repositorio inteiro. A arvore sem
    /// balanceamento vai a 99.999 em tres das cinco entradas, e as tres sao
    /// ordens que aparecem o tempo todo na vida real.
    /// </summary>
    private static void Alturas()
    {
        Console.WriteLine($"== altura, em arestas, com {Quantas:N0} chaves ==");
        Console.Write($"{"entrada",-14}");
        foreach (var nome in Nomes) Console.Write($"{nome,10}");
        Console.WriteLine($"{"limite AVL",12}{"limite RN",12}{"piso",8}");

        var (avl, rubro, minima) = Sequencias.Limites(Quantas);
        foreach (var (entrada, chaves) in Entradas())
        {
            Console.Write($"{entrada,-14}");
            foreach (var nome in Nomes)
            {
                // A arvore sem balanceamento vira lista encadeada e a recursao
                // da travessia estoura a pilha; a altura sai por contagem direta.
                var estrutura = Criar(nome);
                foreach (var chave in chaves) estrutura.Inserir(chave, 0);
                Console.Write($"{estrutura.Altura,10:N0}");
            }
            Console.WriteLine($"{avl,12:F1}{rubro,12:F1}{minima,8:F0}");
        }
        Console.WriteLine();
        Console.WriteLine("as tres entradas do meio sao as mais comuns da vida real: identificadores que");
        Console.WriteLine("crescem, arquivos ja ordenados, carimbos de tempo. A arvore sem balanceamento");
        Console.WriteLine("responde tudo certo nas tres e gasta mais memoria que um vetor para fazer pior");
        Console.WriteLine();
    }

    /// <summary>
    /// Quantas comparacoes custa uma busca, em media, em cada estrutura.
    ///
    /// E a medida que o usuario sente. A altura e o pior caso; isto e o tipico.
    /// </summary>
    private static void Buscas()
    {
        Console.WriteLine($"== comparacoes por busca bem-sucedida, media de 10.000, com {Quantas:N0} chaves ==");
        Console.WriteLine($"{"estrutura",-12}{"sorteada",12}{"crescente",12}{"piso log2(n)",14}");

        var sorteio = new Random(53);
        foreach (var nome in Nomes)
        {
            Console.Write($"{nome,-12}");
            foreach (var entrada in new[] { "sorteada", "crescente" })
            {
                var chaves = entrada == "sorteada"
                    ? Sequencias.Sorteadas(Quantas, 3)
                    : Sequencias.Crescentes(Quantas);

                var estrutura = Criar(nome);
                foreach (var chave in chaves) estrutura.Inserir(chave, 0);

                long total = 0;
                for (var i = 0; i < 10_000; i++)
                {
                    estrutura.Achar(chaves[sorteio.Next(chaves.Count)], out _);
                    total += estrutura.ComparacoesDaUltima;
                }
                Console.Write($"{total / 10_000.0,12:F1}");
            }
            Console.WriteLine($"{Math.Log2(Quantas),14:F1}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// O outro lado da troca: quanto cada estrutura ESCREVE para manter a altura
    /// baixa.
    ///
    /// Rotacao mexe em ponteiro e custa; recoloracao so troca um bit. A AVL fica
    /// mais baixa e a LLRB gira mais, o que e contraintuitivo e e justamente o
    /// numero que vale registrar.
    /// </summary>
    private static void Escritas()
    {
        Console.WriteLine($"== rotacoes por insercao, com {Quantas:N0} chaves ==");
        Console.WriteLine($"{"entrada",-14}{"AVL",12}{"classica",12}{"LLRB",12}{"treap",12}{"LLRB/classica",15}");

        foreach (var (entrada, chaves) in Entradas())
        {
            var avl = new Avl<int, int>();
            foreach (var chave in chaves) avl.Inserir(chave, 0);

            var classica = new Classica<int, int>();
            foreach (var chave in chaves) classica.Inserir(chave, 0);

            var rubro = new Rubro<int, int>();
            foreach (var chave in chaves) rubro.Inserir(chave, 0);

            var treap = new Treap<int, int>(semente: 101);
            foreach (var chave in chaves) treap.Inserir(chave, 0);

            Console.WriteLine($"{entrada,-14}{avl.Rotacoes,12:N0}{classica.Rotacoes,12:N0}" +
                              $"{rubro.Rotacoes,12:N0}{treap.Rotacoes,12:N0}" +
                              $"{rubro.Rotacoes / (double)Math.Max(1, classica.Rotacoes),15:F2}");
        }

        var contador = new Rubro<int, int>();
        foreach (var chave in Sequencias.Sorteadas(Quantas, 3)) contador.Inserir(chave, 0);
        Console.WriteLine();
        Console.WriteLine($"a LLRB ainda faz {contador.Recoloracoes:N0} recoloracoes, que nao mexem em ponteiro nenhum");
        Console.WriteLine("e sao muito mais baratas que uma rotacao");
        Console.WriteLine();
        Console.WriteLine("a ultima coluna e a comparacao que importa: as duas rubro-negras obedecem as");
        Console.WriteLine("MESMAS cinco regras e diferem so na restricao de manter toda vermelha a");
        Console.WriteLine("esquerda. O que essa restricao cobra esta ali, em rotacoes");
        Console.WriteLine();
    }

    /// <summary>
    /// A degeneracao de Hibbard: remover sempre pelo sucessor desequilibra a
    /// arvore sem balanceamento com o tempo.
    ///
    /// Hibbard mostrou isso em 1962 e a conta nao e obvia: a altura media vai
    /// para raiz de n em vez de log n, porque remover sempre pelo sucessor vai
    /// esvaziando o lado esquerdo.
    /// </summary>
    private static void Hibbard()
    {
        Console.WriteLine("== a degeneracao de Hibbard, de 1962 ==");
        Console.WriteLine("n chaves sorteadas, depois ciclos de remover uma viva e inserir uma nova");
        Console.WriteLine($"{"n",8}{"ciclos",12}{"caminho inicial",17}{"caminho final",15}{"2 ln n",10}{"raiz(n)",10}");

        foreach (var n in new[] { 64, 128, 256, 512 })
        {
            var ciclos = 50 * n * n;
            var arvore = new Busca<int, int>();
            var operacoes = Sequencias.Hibbard(n, ciclos, semente: 29);

            var montadas = 0;
            double inicial = 0;
            foreach (var op in operacoes)
            {
                if (op > 0) arvore.Inserir(op, 0); else arvore.Remover(-op);
                if (++montadas == n) inicial = arvore.CaminhoMedio();
            }

            Console.WriteLine($"{n,8}{ciclos,12:N0}{inicial,17:F2}{arvore.CaminhoMedio(),15:F2}" +
                              $"{2 * Math.Log(n),10:F2}{Math.Sqrt(n),10:F2}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// A distribuicao de niveis da lista de pulos: cada nivel com perto de
    /// metade do de baixo, que e de onde vem o tempo logaritmico.
    /// </summary>
    private static void Niveis()
    {
        Console.WriteLine($"== niveis da lista de pulos, com {Quantas:N0} chaves ==");
        Console.WriteLine($"{"nivel",8}{"nos",12}{"razao",10}{"esperado",12}");

        var pulo = new Pulo<int, int>(semente: 101);
        for (var chave = 0; chave < Quantas; chave++) pulo.Inserir(chave, 0);

        var porNivel = pulo.PorNivel();
        for (var nivel = 0; nivel < porNivel.Count; nivel++)
        {
            var razao = nivel == 0 ? 1.0 : porNivel[nivel] / (double)porNivel[nivel - 1];
            Console.WriteLine($"{nivel,8}{porNivel[nivel],12:N0}{razao,10:F3}{Quantas / Math.Pow(2, nivel),12:F0}");
        }
        Console.WriteLine();
        Console.WriteLine($"niveis: {pulo.Niveis}, e log2({Quantas:N0}) e {Math.Log2(Quantas):F1}");
        Console.WriteLine();
    }
}
