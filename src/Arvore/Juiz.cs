namespace Conde.Arvore;

/// <summary>
/// O JUIZ: uma lista ordenada que nao sabe balancear nada.
///
/// Ela guarda os pares num vetor mantido em ordem, por busca binaria e insercao
/// no meio. E O(n) por operacao e e lenta de proposito, e e essa lentidao que a
/// torna confiavel: nao ha caso a analisar, nao ha rotacao que possa estar
/// errada, nao ha cor.
///
/// Um dicionario ordenado errado e especialmente dificil de pegar porque ele
/// quase sempre RESPONDE CERTO. A arvore pode estar completamente desequilibrada
/// e devolver todas as chaves, na ordem certa, com os valores certos. O que
/// quebra e a garantia de altura, e so a invariante denuncia isso. Por isso os
/// testes conferem duas coisas a cada operacao: a resposta contra este juiz, e a
/// invariante da propria estrutura.
/// </summary>
public sealed class Juiz<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private readonly List<(TChave Chave, TValor Valor)> pares = [];

    public int Quantos => pares.Count;

    public long ComparacoesDaUltima { get; private set; }

    public long ComparacoesNoTotal { get; private set; }

    /// <summary>
    /// Onde a chave esta, ou onde ela entraria. Busca binaria, que aqui e so
    /// conveniencia: a corretude nao depende dela.
    /// </summary>
    private int Procurar(TChave chave, out bool achou)
    {
        ComparacoesDaUltima = 0;
        var baixo = 0;
        var alto = pares.Count - 1;
        while (baixo <= alto)
        {
            var meio = (baixo + alto) / 2;
            ComparacoesDaUltima++; ComparacoesNoTotal++;
            var ordem = chave.CompareTo(pares[meio].Chave);
            if (ordem == 0) { achou = true; return meio; }
            if (ordem < 0) alto = meio - 1; else baixo = meio + 1;
        }
        achou = false;
        return baixo;
    }

    public bool Inserir(TChave chave, TValor valor)
    {
        var onde = Procurar(chave, out var achou);
        if (achou) { pares[onde] = (chave, valor); return false; }
        pares.Insert(onde, (chave, valor));
        return true;
    }

    public bool Achar(TChave chave, out TValor valor)
    {
        var onde = Procurar(chave, out var achou);
        valor = achou ? pares[onde].Valor : default!;
        return achou;
    }

    public bool Remover(TChave chave)
    {
        var onde = Procurar(chave, out var achou);
        if (!achou) return false;
        pares.RemoveAt(onde);
        return true;
    }

    public IEnumerable<(TChave Chave, TValor Valor)> EmOrdem() => pares;

    /// <summary>
    /// A altura de uma lista ordenada e a de uma busca binaria sobre ela, que e
    /// o PISO do que qualquer arvore de busca pode fazer.
    ///
    /// Ela esta aqui para servir de referencia: nenhuma estrutura por comparacao
    /// consegue altura menor que log2(n), porque cada comparacao responde um bit
    /// e sao precisos log2(n) bits para apontar um entre n elementos.
    /// </summary>
    public int Altura => pares.Count == 0 ? -1 : (int)Math.Floor(Math.Log2(pares.Count));

    /// <summary>As chaves, em ordem. E a resposta de referencia.</summary>
    public IReadOnlyList<TChave> Chaves() => [.. pares.Select(p => p.Chave)];
}
