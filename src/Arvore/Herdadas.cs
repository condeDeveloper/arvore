namespace Conde.Arvore;

/// <summary>
/// Adaptadores para as duas estruturas que ja estavam neste repositorio antes
/// do contrato <see cref="IDicionario{TChave, TValor}"/> existir: a rubro-negra
/// CLASSICA, com ponteiro para o pai, e uma AVL escrita em outro dia.
///
/// Elas ficaram, e nao por apego. As duas dao exatamente o que faltava para as
/// medidas serem comparacoes de verdade:
///
/// - a rubro-negra classica e a contraparte da inclinada de Sedgewick. Sem ela,
///   a afirmacao "a inclinacao custa rotacoes" so podia ser comparada com a
///   AVL, que tem outra condicao de balanceamento. Com ela, a comparacao e
///   entre duas arvores que obedecem as MESMAS cinco regras e diferem so na
///   restricao extra;
/// - a AVL antiga foi escrita em outro dia, sem olhar para a nova, e as duas
///   obedecem a mesma invariante. Que as duas concordem par a par no dominio
///   inteiro de sete chaves e uma conferencia que nenhuma delas faria sozinha.
///
/// Os adaptadores nao implementam nada: eles so traduzem os nomes. A contagem
/// de comparacoes fica zerada porque as classes antigas nao contam, e e por
/// isso que elas nao aparecem na tabela de comparacoes por busca.
/// </summary>
public sealed class Classica<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private readonly ArvoreRubroNegra<TChave, TValor> arvore = new();

    public int Quantos => arvore.Quantos;

    /// <summary>Quantas rotacoes, que e o numero que esta em comparacao.</summary>
    public long Rotacoes => arvore.Rotacoes;

    public bool Inserir(TChave chave, TValor valor)
    {
        var antes = arvore.Quantos;
        arvore.Por(chave, valor);
        return arvore.Quantos > antes;
    }

    public bool Achar(TChave chave, out TValor valor) => arvore.TentarPegar(chave, out valor);

    public bool Remover(TChave chave) => arvore.Remover(chave);

    public IEnumerable<(TChave Chave, TValor Valor)> EmOrdem() =>
        arvore.EmOrdem().Select(p => (p.Key, p.Value));

    /// <summary>
    /// A classe antiga conta a altura em NOS e devolve zero para a arvore
    /// vazia; o contrato daqui conta em ARESTAS e devolve -1. A traducao e uma
    /// subtracao, e deixar isso implicito seria o jeito mais facil de a tabela
    /// de alturas ficar com uma coluna deslocada em um sem ninguem notar.
    /// </summary>
    public int Altura => arvore.Altura() - 1;

    public long ComparacoesDaUltima => 0;

    public long ComparacoesNoTotal => 0;

    /// <summary>Os problemas que o conferidor da classe antiga encontrou.</summary>
    public IReadOnlyList<string> Conferir() => arvore.Conferir();

    public int AlturaPreta() => arvore.AlturaPreta();
}

/// <summary>
/// A AVL antiga, pelo mesmo contrato. Ela existe aqui para confrontar a nova:
/// duas implementacoes da mesma invariante, escritas em dias diferentes.
/// </summary>
public sealed class AvlAntiga<TChave, TValor> : IDicionario<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private readonly ArvoreAvl<TChave, TValor> arvore = new();

    public int Quantos => arvore.Quantos;

    public long Rotacoes => arvore.Rotacoes;

    public bool Inserir(TChave chave, TValor valor)
    {
        var antes = arvore.Quantos;
        arvore.Por(chave, valor);
        return arvore.Quantos > antes;
    }

    public bool Achar(TChave chave, out TValor valor) => arvore.TentarPegar(chave, out valor);

    public bool Remover(TChave chave) => arvore.Remover(chave);

    public IEnumerable<(TChave Chave, TValor Valor)> EmOrdem() =>
        arvore.EmOrdem().Select(p => (p.Key, p.Value));

    public int Altura => arvore.Altura() - 1;

    public long ComparacoesDaUltima => 0;

    public long ComparacoesNoTotal => 0;

    public IReadOnlyList<string> Conferir() => arvore.Conferir();
}
