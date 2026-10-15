# Recomendação e compatibilidade de hardware | C#

Aplicação CLI com catálogo JSON, enumeração combinatória, restrições de compatibilidade e ranking por custo-benefício ou desempenho. Requer SDK .NET compatível com o alvo `net8.0` (por exemplo, SDK 8 com runtime 8).

## Executar

Abra o terminal nesta pasta:

```sh
dotnet run -- --orcamento 4000 --ram 16 --ssd 500 --top 3 --modo valor
dotnet run -- --orcamento 6000 --modo desempenho
dotnet run -- --self-test
```

Use ponto para centavos no orçamento (`4000.50`). `--catalogo caminho.json` permite trocar o catálogo. `--help` mostra as opções. Não é necessário instalar pacotes NuGet adicionais.

## Conceito: princípio multiplicativo e busca exaustiva

Uma montagem é um elemento do produto cartesiano CPU × placa × RAM × GPU × fonte × SSD × gabinete. Com os tamanhos inclusos:

N = 3 × 3 × 3 × 3 × 2 × 2 × 2 = 648 montagens.

Não usamos combinação C(n,k), pois as categorias têm papéis diferentes. Cada laço escolhe uma peça de uma categoria; toda montagem aparece uma vez. Primeiro filtramos compatibilidade, depois orçamento e capacidades mínimas. Ordenando todas as montagens viáveis, o primeiro item maximiza o critério escolhido **dentro deste catálogo e deste modelo**.

Complexidade: O(N log N) no pior caso por causa da ordenação; O(N) memória. Para catálogos grandes, aplicar poda durante a busca e manter apenas os melhores candidatos reduziria custo.

## Regras implementadas

- Mesmo socket entre CPU e placa-mãe.
- Mesma geração DDR entre CPU, placa e RAM; capacidade de RAM respeitada.
- Suporte M.2 NVMe na placa para os SSDs deste catálogo.
- Formato da placa aceito pelo gabinete e GPU dentro do limite de comprimento.
- Fonte com conectores PCIe de 8 pinos suficientes.
- P_fonte ≥ teto[1,25 × (P_CPU + P_GPU + 70 W)].

A margem de 25% e a reserva fixa de 70 W são **hipóteses didáticas**, não uma norma de dimensionamento. Há uma GPU dedicada em toda montagem; não são enumerados computadores sem GPU.

## Critério de recomendação

As notas de CPU e GPU estão em uma escala comum de 0 a 100. A pontuação é S=0,4S_CPU+0,6S_GPU. O custo-benefício é V=1000S/preço, em pontos por R$1.000. O modo `valor` maximiza V e desempata por S e menor preço. O modo `desempenho` maximiza S e desempata por menor preço.

Os pesos representam um perfil que prioriza GPU, não um benchmark universal. RAM e SSD entram como requisitos mínimos, sem bônus na nota; por isso peças maiores podem não ser escolhidas quando não acrescentam pontuação.

## Limites que precisam ser compreendidos

**Todas as peças, preços e notas são fictícios. Este catálogo não é uma recomendação de compra.** Socket e DDR não garantem compatibilidade real: faltam BIOS, lista de CPUs suportadas, QVL, largura/altura da GPU, refrigeração, cooler, pistas PCIe, interfaces adicionais e qualidade/transientes da fonte. Supõe-se que todas as GPUs usam PCIe compatível, que os kits cabem nos slots, e que todas as CPUs incluem solução térmica adequada. O resultado prova compatibilidade apenas para as regras modeladas.

## Estrutura e demonstração

`Program.cs`: tipos imutáveis (`record`), regras, produto cartesiano, filtros e ordenação LINQ. `catalogo.json`: dados separados da lógica. O autoteste cobre enumeração, socket, DDR, RAM, SSD, formato, comprimento, conectores e potência.

Experimentos: reduza o orçamento para R$1; compare os dois modos; altere a fonte de uma configuração; exija 32 GB e 1000 GB. Explique por que a melhor razão nota/preço pode não consumir todo o orçamento.
