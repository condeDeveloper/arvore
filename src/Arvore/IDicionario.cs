namespace Conde.Arvore;

/// <summary>
/// O contrato de um dicionario ordenado: inserir, achar, remover e percorrer em
/// ordem.
///
/// Todas as estruturas deste repositorio implementam o mesmo contrato, e isso e
/// o que permite rodar a MESMA bateria de testes em todas. O que muda entre
/// elas nao e o que respondem: e o quanto se desequilibram, quanto trabalho
/// gastam para nao se desequilibrar, e o que acontece quando a entrada e
/// adversaria.
/// </summary>
/// <typeparam name="TChave">A chave, que precisa ser comparavel.</typeparam>
/// <typeparam name="TValor">O valor guardado.</typeparam>
public interface IDicionario<TChave, TValor> where TChave : IComparable<TChave>
{
    /// <summary>Quantos pares a estrutura tem.</summary>
    int Quantos { get; }

    /// <summary>
    /// Insere ou substitui. Devolve verdadeiro quando a chave e nova.
    /// </summary>
    bool Inserir(TChave chave, TValor valor);

    /// <summary>Procura a chave.</summary>
    bool Achar(TChave chave, out TValor valor);

    /// <summary>Remove a chave. Devolve verdadeiro quando ela existia.</summary>
    bool Remover(TChave chave);

    /// <summary>Os pares em ordem crescente de chave.</summary>
    IEnumerable<(TChave Chave, TValor Valor)> EmOrdem();

    /// <summary>
    /// A altura da estrutura, em arestas, ou -1 quando ela esta vazia.
    ///
    /// Nao faz parte do contrato de um dicionario de verdade, e esta aqui porque
    /// e exatamente o que se quer MEDIR. Uma arvore que responde certo e tem
    /// altura n nao esta errada, esta inutil, e so a altura denuncia isso.
    /// </summary>
    int Altura { get; }

    /// <summary>
    /// Quantas comparacoes de chave a ultima operacao gastou.
    ///
    /// E a medida que nao depende de maquina. Tempo de relogio depende de cache,
    /// de frequencia e de quem mais esta rodando; contagem de comparacao da o
    /// mesmo numero em qualquer lugar, e e ela que carrega a conclusao.
    /// </summary>
    long ComparacoesDaUltima { get; }

    /// <summary>Comparacoes desde que a estrutura existe.</summary>
    long ComparacoesNoTotal { get; }
}
