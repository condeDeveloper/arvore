namespace Conde.Arvore;

/// <summary>
/// As sequencias de chaves.
///
/// Elas existem porque arvore de busca nao tem um "caso medio" que valha para
/// tudo: o que decide a altura e a ORDEM DE CHEGADA das chaves, e as ordens que
/// aparecem na vida real nao sao sorteadas. Identificadores crescem. Registros
/// vem de arquivos ja ordenados. Carimbos de tempo so aumentam.
///
/// Uma arvore testada so com chaves sorteadas esta testada contra a entrada que
/// ela nunca vai receber.
/// </summary>
public static class Sequencias
{
    /// <summary>
    /// Chaves sorteadas, sem repetir. E o caso facil, e o unico em que a arvore
    /// sem balanceamento funciona.
    /// </summary>
    public static List<int> Sorteadas(int quantas, int semente)
    {
        var sorteio = new Random(semente);
        var vistas = new HashSet<int>();
        var saida = new List<int>(quantas);
        while (saida.Count < quantas)
        {
            var chave = sorteio.Next(quantas * 10);
            if (vistas.Add(chave)) saida.Add(chave);
        }
        return saida;
    }

    /// <summary>
    /// Chaves em ordem crescente, que e o caso mais comum do mundo e o pior
    /// possivel para uma arvore sem balanceamento: ela vira uma lista
    /// encadeada, com altura n-1.
    /// </summary>
    public static List<int> Crescentes(int quantas) => [.. Enumerable.Range(0, quantas)];

    public static List<int> Decrescentes(int quantas) => [.. Enumerable.Range(0, quantas).Reverse()];

    /// <summary>
    /// Zigue-zague: menor, maior, segundo menor, segundo maior, e assim por
    /// diante.
    ///
    /// Tambem produz uma lista encadeada na arvore sem balanceamento, e por um
    /// caminho diferente do crescente: cada chave nova vai para o lado oposto
    /// da anterior. Quem conserta so o caso crescente continua quebrado aqui.
    /// </summary>
    public static List<int> ZigueZague(int quantas)
    {
        var saida = new List<int>(quantas);
        int baixo = 0, alto = quantas - 1;
        while (baixo <= alto)
        {
            saida.Add(baixo++);
            if (baixo <= alto) saida.Add(alto--);
        }
        return saida;
    }

    /// <summary>
    /// Blocos ordenados embaralhados entre si: o formato de dados que vieram de
    /// varios arquivos ordenados, concatenados.
    ///
    /// E o caso que mais aparece de verdade e que quase nunca e testado, porque
    /// ele nao e nem sorteado nem ordenado.
    /// </summary>
    public static List<int> EmBlocos(int quantas, int tamanhoDoBloco, int semente)
    {
        var sorteio = new Random(semente);
        var blocos = new List<List<int>>();
        for (var inicio = 0; inicio < quantas; inicio += tamanhoDoBloco)
            blocos.Add([.. Enumerable.Range(inicio, Math.Min(tamanhoDoBloco, quantas - inicio))]);

        var saida = new List<int>(quantas);
        foreach (var bloco in blocos.OrderBy(_ => sorteio.Next())) saida.AddRange(bloco);
        return saida;
    }

    /// <summary>
    /// A sequencia de Hibbard: monta a arvore com <paramref name="quantas"/>
    /// chaves SORTEADAS e depois alterna remover uma viva e inserir uma nova,
    /// mantendo a populacao parada.
    ///
    /// Hibbard mostrou em 1962 que isso desequilibra uma arvore de busca sem
    /// balanceamento quando a remocao substitui SEMPRE pelo sucessor: o
    /// comprimento medio de caminho vai de 2 ln n para a ordem de raiz de n,
    /// porque tirar sempre pela direita vai pendurando a arvore para a esquerda.
    ///
    /// As duas condicoes importam e eu errei as duas na primeira versao. As
    /// chaves iniciais precisam ser SORTEADAS: com chaves em ordem crescente a
    /// arvore ja nasce lista encadeada, e a medida mostra a degeneracao da
    /// ordem de chegada, nao a de Hibbard. E a populacao precisa ficar PARADA:
    /// sorteando entre inserir e remover com a mesma chance, a quantidade de
    /// chaves vagueia e a primeira medida terminou com 116 chaves de dez mil
    /// operacoes.
    ///
    /// Devolve a lista de operacoes: positivo insere, negativo remove.
    /// </summary>
    public static List<int> Hibbard(int quantas, int ciclos, int semente)
    {
        var sorteio = new Random(semente);
        // O universo precisa ser folgado em relacao ao TOTAL de chaves que vao
        // nascer, e nao a populacao. Dimensionar pela populacao foi o terceiro
        // erro desta funcao: com mil chaves vivas e duzentos e cinquenta mil
        // ciclos, o sorteio de chave inedita entra em laco infinito.
        var universo = (quantas + ciclos) * 4;
        var vivas = new List<int>();
        var usadas = new HashSet<int>();
        var saida = new List<int>(quantas + 2 * ciclos);

        int Nova()
        {
            while (true)
            {
                var chave = sorteio.Next(universo);
                if (usadas.Add(chave)) return chave;
            }
        }

        for (var i = 0; i < quantas; i++)
        {
            var chave = Nova();
            vivas.Add(chave);
            saida.Add(chave);
        }

        for (var i = 0; i < ciclos; i++)
        {
            var k = sorteio.Next(vivas.Count);
            saida.Add(-vivas[k]);
            vivas.RemoveAt(k);

            var chave = Nova();
            vivas.Add(chave);
            saida.Add(chave);
        }
        return saida;
    }

    /// <summary>
    /// O limite teorico da altura, em arestas, para cada estrutura, com n
    /// chaves.
    ///
    /// Estes numeros nao sao estimativas: sao os limites provados, e e contra
    /// eles que o medidor confere o que mediu.
    /// </summary>
    public static (double Avl, double Rubro, double Minima) Limites(int quantas)
    {
        if (quantas <= 0) return (0, 0, 0);
        // AVL: a arvore minima de altura h tem Fibonacci(h+3) - 1 nos, o que da
        // h <= 1,4405 log2(n+2) - 1,3277.
        var avl = 1.4405 * Math.Log2(quantas + 2) - 1.3277;
        // Rubro-negra: 2 log2(n+1), em arestas.
        var rubro = 2 * Math.Log2(quantas + 1);
        // E o piso de qualquer estrutura por comparacao.
        var minima = Math.Floor(Math.Log2(quantas));
        return (avl, rubro, minima);
    }
}
