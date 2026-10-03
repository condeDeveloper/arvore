# arvore

Dicionários ordenados do zero em C# e .NET 8: árvore de busca sem
balanceamento, AVL, rubro-negra inclinada para a esquerda, treap e lista de
pulos. O juiz é uma lista ordenada que não sabe balancear nada, e a invariante
de cada estrutura é conferida depois de **cada** operação.

```
$ dotnet medidor.dll busca

== comparacoes por busca bem-sucedida, media de 10.000, com 100,000 chaves ==
estrutura       sorteada   crescente  piso log2(n)
busca               20.4     49813.2          16.6
avl                 16.0        15.7          16.6
rubro               16.2        15.7          16.6
treap               20.5        21.8          16.6
pulo                30.9        31.8          16.6
```

A árvore sem balanceamento gasta **49.813 comparações** por busca quando as
chaves chegaram em ordem. Ela responde tudo certo. É três mil vezes a AVL.

## Responder certo não é o problema

Um dicionário ordenado errado é especialmente difícil de pegar porque ele quase
sempre **responde certo**. Ele devolve as chaves, na ordem, com os valores
certos. O que quebrou foi a garantia de altura, e nenhuma resposta denuncia
isso.

Por isso aqui há dois juízes ao mesmo tempo, e o segundo é o que importa.

O primeiro é uma lista ordenada que responde pela definição, sem rotação, sem
cor e sem caso a analisar — e ela mesma é conferida contra o `SortedDictionary`
da plataforma, que é uma rubro-negra escrita por outra gente.

O segundo é a **invariante de cada estrutura**, conferida depois de cada
operação: o equilíbrio e a altura guardada na AVL, as quatro regras da
rubro-negra, a propriedade de monte na treap, e o fato de cada nível da lista
de pulos ser um subconjunto ordenado do nível de baixo. Comparar o resultado
final pega o erro quando ele já virou resposta errada; conferir a cada passo
pega a hora em que ele nasce.

E a comparação não é por amostra: todas as **5.040** ordens de inserção de 7
chaves, cada uma seguida de remoção em ordem embaralhada; e, para a remoção da
rubro-negra, que é a parte difícil, todos os pares de ordem de inserção e de
remoção até 5 chaves — 14.400 pares por tamanho.

Há ainda um terceiro confronto, que é o mais barato de todos: **duas
implementações independentes da mesma coisa**. A rubro-negra clássica, com
ponteiro para o pai, e a inclinada, sem ele; a AVL nova e uma AVL escrita em
outro dia. Elas foram escritas sem olhar uma para a outra e obedecem às mesmas
invariantes, então concordarem par a par é uma conferência que nenhuma delas
faria sozinha.

## A tabela que justifica o repositório

```
$ dotnet medidor.dll altura

== altura, em arestas, com 100,000 chaves ==
entrada            busca       avl     rubro     treap      pulo  limite AVL   limite RN    piso
sorteada              36        19        22        36        15        22.6        33.2      16
crescente         99,999        16        16        42        15        22.6        33.2      16
decrescente       99,999        16        21        42        15        22.6        33.2      16
zigue-zague       99,999        20        21        42        15        22.6        33.2      16
em blocos          1,304        18        21        38        15        22.6        33.2      16
```

As três entradas do meio não são casos de borda: são as ordens mais comuns que
existem. Identificadores que crescem. Registros importados de um arquivo
ordenado. Carimbos de tempo. Uma árvore de busca testada só com chaves
sorteadas está testada contra a entrada que ela nunca vai receber.

Repare na linha "em blocos", que é o formato de dados vindos de vários arquivos
ordenados concatenados: 1.304. Não é a catástrofe das outras três e é oitenta
vezes o logaritmo. É o caso que ninguém testa porque ele não é nem sorteado nem
ordenado.

E repare que a entrada crescente é o **melhor** caso da AVL e da rubro-negra:
altura 16, que é exatamente o piso de qualquer estrutura por comparação. A mesma
entrada que destrói uma estrutura deixa a outra perfeita.

A coluna da lista de pulos conta níveis e não arestas de árvore, então ela não é
comparável com as outras nessa escala. A medida comparável dela é a de
comparações, logo acima: 31, contra 16 das árvores balanceadas. A simplicidade
do código custa perto do dobro de comparações.

## O que as medidas me corrigiram

**A árvore degenerada estoura a pilha de quem tenta medi-la.** A primeira versão
de `Altura` era recursiva, de uma linha. Ela morreu ao medir a árvore de cem mil
chaves em ordem crescente, com 24.081 quadros empilhados. O erro é didático,
porque ele **é** o assunto: uma árvore degenerada não fica só lenta, ela fica
funda demais para qualquer algoritmo recursivo que ande nela, inclusive os que
servem para diagnosticá-la. Nas balanceadas o problema não existe, e por isso só
essa classe precisou de pilha própria.

**A degeneração de Hibbard é real e invisível na escala em que se testa.** Em
1962 Hibbard mostrou que remover sempre pelo sucessor piora a árvore com o
tempo. Eu ia escrever isso como fato e medi o contrário: com mil chaves e 256
mil ciclos de atualização, o comprimento médio de caminho **caiu** de 11,5 para
9,9. O efeito só aparece no regime quadrático:

```
$ dotnet medidor.dll hibbard

       n      ciclos  caminho inicial  caminho final    2 ln n   raiz(n)
      64     204,800             5.42           5.41      8.32      8.00
     128     819,200             6.97           7.33      9.70     11.31
     256   3,276,800             8.59           8.34     11.09     16.00
     512  13,107,200            10.10          12.27     12.48     22.63
```

Com 64 chaves e duzentos mil ciclos, nada. Com 512 e treze milhões, o caminho
médio sobe 21%. São precisas da ordem de n² atualizações, e é por isso que o
fenômeno é tão fácil de não ver.

**E eu errei três vezes a própria montagem desse experimento.** A primeira
versão inseria chaves em ordem crescente, então a árvore já nascia lista
encadeada e a medida mostrava a degeneração da ordem de chegada, não a de
Hibbard. A segunda sorteava entre inserir e remover com a mesma chance, o que é
um passeio aleatório: a população vagueava, e dez mil operações terminaram com
116 chaves. A terceira dimensionou o universo de chaves pela população em vez do
total, e o sorteio de chave inédita entrou em laço infinito.

**Eu estava comparando as coisas erradas para medir o preço da inclinação.** A
primeira versão comparava a LLRB com a AVL, e isso mistura dois efeitos: as duas
obedecem a condições de balanceamento diferentes, então a diferença de rotações
não isola nada. A comparação honesta é com a rubro-negra **clássica**, que
obedece às mesmas cinco regras e difere só na restrição de manter toda ligação
vermelha à esquerda:

```
$ dotnet medidor.dll escrita

entrada                AVL    classica        LLRB       treap  LLRB/classica
sorteada            70,068      58,186     118,499     199,896           2.04
crescente           99,983      99,969      99,984      99,990           1.00
decrescente         99,983      99,969      99,978      99,990           1.00
zigue-zague        162,462     149,958     199,968     200,346           1.33
em blocos          134,223     181,430     185,640     201,660           1.02
```

A inclinação custa **o dobro das rotações** com chaves sorteadas. Em troca, a
inserção inteira cabe em três linhas. E a coluna da clássica mostra a outra
troca, a que separa as duas famílias: ela gira menos que a AVL (58 mil contra
70 mil) e fica mais alta, que é exatamente o que uma condição de balanceamento
mais frouxa compra e paga.

## A ideia de cada uma

**AVL**, 1962, a primeira estrutura auto-balanceada da história: as alturas das
duas subárvores diferem no máximo em um. O limite sai de uma recorrência bonita
— a AVL mínima de altura h tem subárvores de altura h-1 e h-2, que é Fibonacci,
e daí a altura é no máximo 1,44 log2(n).

**Rubro-negra**, aqui na variante inclinada para a esquerda de Sedgewick: é uma
árvore 2-3 disfarçada de binária, com as ligações vermelhas representando os nós
de três chaves. Se todo caminho tem a mesma quantidade de ligações pretas e não
há duas vermelhas seguidas, o caminho mais longo é no máximo o dobro do mais
curto, e a altura fica em 2 log2(n+1).

**Treap**, 1996: uma árvore de busca pela chave e um monte pela prioridade, que
é sorteada. O resultado é desconcertante de tão simples — a árvore tem
exatamente a forma que teria se as chaves tivessem chegado em ordem aleatória,
**qualquer que tenha sido a ordem real**. Não há caso a analisar, não há altura
guardada, não há cor. Um dos testes exibe isso: as mesmas chaves com as mesmas
prioridades, inseridas em ordem crescente, decrescente, em zigue-zague e
embaralhada, dão a mesma árvore, nó por nó.

**Lista de pulos**, 1990, que nem árvore é: listas encadeadas ordenadas
empilhadas, cada nível com metade dos elementos do de baixo, e a altura de cada
elemento vindo de cara ou coroa. Pugh escreveu o artigo com o argumento de que
ela é mais simples de implementar certo, e implementar certo é a parte difícil.
A moeda sai exata:

```
$ dotnet medidor.dll niveis

   nivel         nos     razao    esperado
       0     100,000     1.000      100000
       1      49,771     0.498       50000
       2      24,910     0.500       25000
       3      12,447     0.500       12500
       4       6,192     0.497        6250
       5       3,048     0.492        3125
```

## As peças

| arquivo | o que faz |
| --- | --- |
| `IDicionario.cs` | o contrato comum, com altura e contagem de comparações |
| `Busca.cs` | a árvore sem balanceamento, que é a linha de base e o problema |
| `Avl.cs` | AVL, com os quatro casos de desequilíbrio |
| `Rubro.cs` | rubro-negra inclinada para a esquerda, inserção e remoção |
| `Treap.cs` | árvore de busca pela chave, monte pela prioridade sorteada |
| `Pulo.cs` | lista de pulos, com os níveis vindos de cara ou coroa |
| `Juiz.cs` | a lista ordenada que responde pela definição |
| `Sequencias.cs` | as ordens de chegada, e os limites provados de cada estrutura |
| `ArvoreRubroNegra.cs` | a rubro-negra clássica, com ponteiro para o pai |
| `ArvoreAvl.cs` | uma AVL escrita em outro dia, que serve de segunda opinião |
| `Herdadas.cs` | os adaptadores que trazem as duas para o contrato comum |

## Como rodar

```
dotnet test testes/Arvore.Testes/Arvore.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `altura`, `busca`, `escrita`, `hibbard`, `niveis` e `tudo`.

## Licença

MIT.
