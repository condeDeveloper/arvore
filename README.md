# arvore

Uma árvore rubro-negra do zero em C# e .NET 8 — a estrutura que segura o
`SortedDictionary` do .NET, o `TreeMap` do Java e o `std::map` do C++.

```
$ dotnet medidor.dll altura

       nós   ordenadas   sorteadas    teto   ideal  (numa árvore sem balanço)
------------------------------------------------------------------------------
        10           5           4       8       4   10
       100          11           8      14       7   100
      1000          17          12      20      10   1000
     10000          24          16      28      14   10000
    100000          31          20      34      17   100000
   1000000          37          25      40      20   1000000
```

A última coluna é o que uma árvore de busca **sem** balanceamento teria com
chaves ordenadas: uma corrente de um milhão de nós, e a busca custando um milhão
de passos em vez de 37.

E dados ordenados não são caso raro — são o **comum**. Chaves que chegam de um
banco, de um arquivo, de um contador: quase tudo chega ordenado, e é justamente
aí que a árvore ingênua desaba.

## Os dois juízes

**O `SortedDictionary` do .NET**, que é, ele mesmo, uma árvore rubro-negra — a
comparação é contra uma implementação madura da **mesma** estrutura, exata e
ilimitada. São **100.000 operações sorteadas** (inserir, remover, buscar,
conferir) com os dois lado a lado.

**As próprias cinco regras**, conferidas depois de **cada** operação.

E o segundo é mais importante que o primeiro, por um motivo que vale dizer: uma
árvore rubro-negra com um caso de remoção errado **continua funcionando**. Ela
guarda, busca e devolve tudo certo; só vai ficando torta, e a busca que custava
20 passos passa a custar mil. O `SortedDictionary` nunca pegaria isso — as duas
respondem igual.

O verificador é o que transforma *"a árvore funciona"* em *"a árvore é uma
rubro-negra"*.

## As cinco regras

1. A raiz é preta.
2. As folhas (os nulos) são pretas.
3. Um nó vermelho tem os dois filhos pretos — **nunca dois vermelhos seguidos**.
4. Todo caminho da raiz a uma folha passa pelo **mesmo número** de nós pretos.
5. (A cor é uma informação só: um bit por nó.)

As regras 3 e 4 juntas dão o resultado: **o caminho mais longo tem no máximo o
dobro do mais curto**. Isso basta para garantir altura `O(log n)`, e é muito mais
barato de manter que o equilíbrio perfeito de uma AVL.

## As rotações, medidas

```
$ dotnet medidor.dll rotacoes

   operações   inserções  por inserção    remoções   por remoção
----------------------------------------------------------------
        1000         677         0.328         323         0.127
      100000       66383         0.337       33617         0.126
     1000000      665710         0.336       334290        0.123
```

A promessa é **no máximo 2 por inserção e 3 por remoção**, independentemente do
tamanho — e o que se mede é 0,34 e 0,12: a maioria das operações não rotaciona
nada.

## A frase do livro que eu li errado

O projeto tem também uma **AVL**, e ela está aqui para que a comparação seja
medida em vez de afirmada. Todo texto sobre estruturas diz alguma versão de *"a
AVL é mais equilibrada e a rubro-negra rotaciona menos"*, e eu escrevi um teste
afirmando a segunda metade.

**Ele falhou.** Em 50 mil remoções sorteadas:

| | rotações totais | média por remoção | pior remoção |
|---|---|---|---|
| AVL | 18.719 | 0,374 | **mais de 3** |
| rubro-negra | 18.941 | 0,379 | **3, sempre** |

A rubro-negra rotacionou **mais** no total. A frase do livro não estava errada —
a minha leitura dela estava. O `O(log n)` da AVL é **pior caso**, e na média as
duas gastam menos de meia rotação por remoção.

A diferença existe e está em outro lugar: no **máximo que uma remoção pode
custar**. A rubro-negra tem teto de três, e ele vale sempre; a AVL não tem teto
constante nenhum. Para um sistema em que a latência do pior caso importa — um
escalonador, um banco de dados — isso é a diferença toda. Para a média, não é
nada.

O teste agora mede as duas coisas e afirma as duas: que a rubro-negra **nunca**
passa de três, que a AVL passa, e que as médias ficam a menos de 0,3 uma da
outra.

E a outra metade da frase, essa sim, se confirmou: a AVL fica mais baixa. Com
cem mil chaves ordenadas, altura 20 contra 31.

## A rotação, que é a única coisa que dá para fazer

```
     x                y
    / \              / \
   a   y    ──►     x   c
      / \          / \
     b   c        a   b

  a x b y c   =   a x b y c
```

Numa árvore de busca, o percurso em ordem é a sequência ordenada das chaves — e a
rotação **não muda esse percurso**. As duas árvores contêm a mesma coisa na mesma
ordem, com alturas diferentes.

Todo balanceamento — rubro-negra, AVL, splay, treap — é construído em cima disto.

## A remoção é a parte que ninguém acerta

A inserção tem **três** casos, e só um deles sobe na árvore. A remoção tem
**quatro**, e a cascata é sobre a cor do irmão e dos dois sobrinhos:

| caso | o que acontece |
|---|---|
| irmão vermelho | uma rotação o troca por um preto; cai nos de baixo |
| irmão preto, dois sobrinhos pretos | pinta o irmão de vermelho; o problema **sobe** |
| irmão preto, sobrinho de fora preto | uma rotação traz o vermelho para fora |
| irmão preto, sobrinho de fora vermelho | uma rotação no pai resolve, e **acaba** |

Só o caso 2 sobe, e ele não rotaciona. Os outros rotacionam uma vez cada e o 4
encerra — daí o teto de três.

E há o truque que reduz o problema: um nó com **dois** filhos nunca é removido.
Ele recebe a chave do sucessor, e quem sai é o sucessor — que tem no máximo um
filho, por construção.

## Contra o `SortedDictionary`

```
operação                   nós         meu        .NET     razão
----------------------------------------------------------------
inserir                  10000      4.3 ms     10.1 ms      0.4x
inserir                 100000     41.0 ms     78.8 ms      0.5x
inserir                1000000   2596.8 ms   3012.1 ms      0.9x
buscar                 1000000   1454.6 ms   1516.8 ms      1.0x
percorrer em ordem     1000000    173.1 ms    181.0 ms      1.0x
```

Este é o resultado que eu não esperava: a implementação daqui é **mais rápida**
que a do .NET na inserção, e empata no resto.

A explicação honesta não é que o código é melhor — é que ele faz **menos**. O
`SortedDictionary` compara através de um `IComparer<T>`, que é uma chamada
virtual por comparação e não pode ser inlinada; aqui a restrição genérica
`where TChave : IComparable<TChave>` permite ao compilador especializar a chamada
para o tipo. A diferença some no caso de um milhão de nós, onde o custo passa a
ser dominado pelas faltas de cache.

O preço é que esta árvore **não aceita um comparador**: ela ordena do jeito que o
tipo ordena, e ponto. É uma funcionalidade a menos, não uma otimização.

## Rodar

.NET 8. Zero dependências fora do xUnit, e só nos testes.

```
dotnet test testes/Arvore.Testes/Arvore.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release
```

## O que ele não faz

Não aceita comparador, como está dito acima. Não tem busca por faixa
(`entre(a, b)`), nem o k-ésimo elemento — as duas exigiriam guardar o tamanho da
subárvore em cada nó, o que é fácil e muda a estrutura. Não é thread-safe, nem
persistente, nem tem iterador que sobreviva a uma modificação. E não é uma
B-tree: para dados em disco, onde o custo é a leitura de página e não a
comparação, a resposta é outra estrutura.

## Licença

MIT.
