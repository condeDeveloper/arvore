namespace Conde.Arvore;

/// <summary>
/// Uma árvore AVL — a outra resposta ao mesmo problema.
/// </summary>
/// <remarks>
/// <para>
/// Ela está aqui para que a comparação com a rubro-negra seja <b>medida</b>, e
/// não afirmada. Todo texto sobre estruturas diz que "a AVL é mais equilibrada e
/// a rubro-negra rotaciona menos", e o número de vezes que esse texto vem com o
/// número junto é pequeno.
/// </para>
/// <para>
/// A AVL é de 1962 — Adelson-Velsky e Landis, em Moscou — e é a primeira
/// estrutura de dados auto-balanceada da história. A regra é mais simples que a
/// da rubro-negra e é só uma:
/// </para>
/// <code>
///   a diferença de altura entre os dois filhos de qualquer nó é -1, 0 ou 1
/// </code>
/// <para>
/// Uma regra em vez de cinco, e ela dá um equilíbrio <b>melhor</b>: a altura de
/// uma AVL fica em torno de 1,44·log₂(n), contra 2·log₂(n) da rubro-negra. Busca
/// mais rápida.
/// </para>
/// <para>
/// O preço aparece na remoção. Numa rubro-negra, tirar um nó custa no máximo
/// três rotações; numa AVL, o reequilíbrio pode ter de subir <b>até a raiz</b>,
/// rotacionando em cada nível — <c>O(log n)</c> rotações. A ferramenta de medida
/// mostra as duas curvas lado a lado.
/// </para>
/// </remarks>
public sealed class ArvoreAvl<TChave, TValor>
    where TChave : IComparable<TChave>
{
    private sealed class No(TChave chave, TValor valor)
    {
        public readonly TChave Chave = chave;
        public TValor Valor = valor;

        public No? Esquerda;
        public No? Direita;

        // A altura fica guardada em vez de calculada. Calcular custaria O(n) por
        // consulta, e a regra da AVL precisa dela a cada passo do reequilíbrio.
        public int Altura = 1;
    }

    private No? _raiz;

    public int Quantos { get; private set; }

    public long Rotacoes { get; private set; }

    public bool Contem(TChave chave) => Achar(_raiz, chave) is not null;

    public bool TentarPegar(TChave chave, out TValor valor)
    {
        var no = Achar(_raiz, chave);

        if (no is null)
        {
            valor = default!;

            return false;
        }

        valor = no.Valor;

        return true;
    }

    private static No? Achar(No? no, TChave chave)
    {
        while (no is not null)
        {
            var comparacao = chave.CompareTo(no.Chave);

            if (comparacao == 0)
            {
                return no;
            }

            no = comparacao < 0 ? no.Esquerda : no.Direita;
        }

        return null;
    }

    public void Por(TChave chave, TValor valor)
    {
        var antes = Quantos;

        _raiz = Inserir(_raiz, chave, valor);

        if (Quantos == antes)
        {
            // A chave já existia: o valor foi trocado e nada mais.
        }
    }

    private No Inserir(No? no, TChave chave, TValor valor)
    {
        if (no is null)
        {
            Quantos++;

            return new No(chave, valor);
        }

        var comparacao = chave.CompareTo(no.Chave);

        if (comparacao == 0)
        {
            no.Valor = valor;

            return no;
        }

        if (comparacao < 0)
        {
            no.Esquerda = Inserir(no.Esquerda, chave, valor);
        }
        else
        {
            no.Direita = Inserir(no.Direita, chave, valor);
        }

        return Reequilibrar(no);
    }

    public bool Remover(TChave chave)
    {
        var antes = Quantos;

        _raiz = Remover(_raiz, chave);

        return Quantos < antes;
    }

    private No? Remover(No? no, TChave chave)
    {
        if (no is null)
        {
            return null;
        }

        var comparacao = chave.CompareTo(no.Chave);

        if (comparacao < 0)
        {
            no.Esquerda = Remover(no.Esquerda, chave);
        }
        else if (comparacao > 0)
        {
            no.Direita = Remover(no.Direita, chave);
        }
        else
        {
            Quantos--;

            if (no.Esquerda is null)
            {
                return no.Direita;
            }

            if (no.Direita is null)
            {
                return no.Esquerda;
            }

            // Dois filhos: o sucessor toma o lugar, e é ele que sai de verdade.
            var sucessor = no.Direita;

            while (sucessor.Esquerda is not null)
            {
                sucessor = sucessor.Esquerda;
            }

            var novo = new No(sucessor.Chave, sucessor.Valor)
            {
                Esquerda = no.Esquerda,
                Direita = no.Direita,
            };

            Quantos++;

            novo.Direita = Remover(novo.Direita, sucessor.Chave);

            no = novo;
        }

        return Reequilibrar(no);
    }

    /// <summary>
    /// O reequilíbrio: quatro casos, e a diferença de altura decide.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A regra da AVL é uma só, e o conserto também é curto. Se um lado ficou
    /// dois níveis mais alto que o outro, há quatro formas de arrumar, conforme
    /// de que lado veio o desequilíbrio:
    /// </para>
    /// <code>
    ///   esquerda-esquerda   uma rotação à direita
    ///   esquerda-direita    uma rotação à esquerda no filho, depois à direita
    ///   direita-direita     uma rotação à esquerda
    ///   direita-esquerda    uma à direita no filho, depois à esquerda
    /// </code>
    /// <para>
    /// A diferença para a rubro-negra está em <b>quantas vezes</b> isto
    /// acontece: aqui, a cada nível do caminho de volta, até a raiz. É a origem
    /// do <c>O(log n)</c> rotações na remoção — e é também o que dá o
    /// equilíbrio melhor.
    /// </para>
    /// </remarks>
    private No Reequilibrar(No no)
    {
        Ajustar(no);

        var diferenca = Diferenca(no);

        if (diferenca > 1)
        {
            // Pendendo para a esquerda.
            if (Diferenca(no.Esquerda!) < 0)
            {
                no.Esquerda = GirarParaEsquerda(no.Esquerda!);
            }

            return GirarParaDireita(no);
        }

        if (diferenca < -1)
        {
            if (Diferenca(no.Direita!) > 0)
            {
                no.Direita = GirarParaDireita(no.Direita!);
            }

            return GirarParaEsquerda(no);
        }

        return no;
    }

    private static int AlturaDe(No? no) => no?.Altura ?? 0;

    private static void Ajustar(No no) =>
        no.Altura = 1 + Math.Max(AlturaDe(no.Esquerda), AlturaDe(no.Direita));

    private static int Diferenca(No no) => AlturaDe(no.Esquerda) - AlturaDe(no.Direita);

    private No GirarParaDireita(No y)
    {
        var x = y.Esquerda!;

        y.Esquerda = x.Direita;
        x.Direita = y;

        Ajustar(y);
        Ajustar(x);

        Rotacoes++;

        return x;
    }

    private No GirarParaEsquerda(No x)
    {
        var y = x.Direita!;

        x.Direita = y.Esquerda;
        y.Esquerda = x;

        Ajustar(x);
        Ajustar(y);

        Rotacoes++;

        return y;
    }

    public int Altura() => AlturaDe(_raiz);

    public IEnumerable<KeyValuePair<TChave, TValor>> EmOrdem()
    {
        var pilha = new Stack<No>();
        var atual = _raiz;

        while (atual is not null || pilha.Count > 0)
        {
            while (atual is not null)
            {
                pilha.Push(atual);
                atual = atual.Esquerda;
            }

            atual = pilha.Pop();

            yield return new KeyValuePair<TChave, TValor>(atual.Chave, atual.Valor);

            atual = atual.Direita;
        }
    }

    /// <summary>Confere a regra única da AVL, e a ordem.</summary>
    public IReadOnlyList<string> Conferir()
    {
        var problemas = new List<string>();
        var contados = 0;

        Conferir(_raiz, problemas, ref contados);

        if (contados != Quantos)
        {
            problemas.Add($"a contagem diz {Quantos} e há {contados} nós");
        }

        TChave? anterior = default;
        var primeiro = true;

        foreach (var par in EmOrdem())
        {
            if (!primeiro && anterior!.CompareTo(par.Key) >= 0)
            {
                problemas.Add($"fora de ordem: {anterior} antes de {par.Key}");
            }

            anterior = par.Key;
            primeiro = false;
        }

        return problemas;
    }

    private static int Conferir(No? no, List<string> problemas, ref int contados)
    {
        if (no is null)
        {
            return 0;
        }

        contados++;

        var esquerda = Conferir(no.Esquerda, problemas, ref contados);
        var direita = Conferir(no.Direita, problemas, ref contados);

        var diferenca = esquerda - direita;

        if (diferenca is < -1 or > 1)
        {
            problemas.Add($"desequilíbrio de {diferenca} em {no.Chave}");
        }

        var altura = 1 + Math.Max(esquerda, direita);

        if (no.Altura != altura)
        {
            problemas.Add($"a altura de {no.Chave} diz {no.Altura} e é {altura}");
        }

        return altura;
    }
}
