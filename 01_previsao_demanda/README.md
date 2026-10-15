# Previsão de demanda por regressão linear | Python

Aplicação de terminal que lê vendas mensais, avalia uma tendência linear em dados posteriores e projeta os próximos meses. Somente biblioteca padrão. Python 3.10 ou superior.

## Executar

Abra o terminal nesta pasta:

```sh
python previsao.py
python previsao.py --entrada dados/vendas.csv --meses 6 --saida resultados
python -m unittest -v
```

No Linux, o comando pode ser `python3`. O CSV incluído contém **36 meses sintéticos**, não dados reais de uma empresa. Troque-o por seu histórico para uma aplicação real. Vendas estão em unidades, não em reais. A previsão de vendas aproxima demanda apenas quando não houve rupturas de estoque: vendas observadas podem ser menores que a demanda real.

Formato obrigatório: `data,vendas`, com datas `AAAA-MM-01`, meses consecutivos, sem duplicações, quantidades não negativas e pelo menos oito meses. O programa ordena os registros por data. O horizonte é de 1 a 120 meses; horizontes longos aumentam a incerteza.

## Método matemático

Conceito aplicado: **mínimos quadrados**, um procedimento de ajuste numérico. Não se aplica bisseção ou Newton artificialmente: a solução da reta possui forma fechada.

Para os pares (xᵢ,yᵢ), minimizar S(a,b)=Σ[yᵢ−a−bxᵢ]². Igualando as derivadas a zero:

- ∂S/∂a=−2Σ(yᵢ−a−bxᵢ)=0, portanto a=ȳ−b x̄.
- ∂S/∂b=−2Σxᵢ(yᵢ−a−bxᵢ)=0.
- Substituindo a: b=Σ(xᵢ−x̄)(yᵢ−ȳ)/Σ(xᵢ−x̄)².

Quando Σ(xᵢ−x̄)²>0, as colunas 1 e x são linearmente independentes e a Hessiana 2XᵀX é positiva definida. Portanto o ponto encontrado é o mínimo global único. A implementação usa meses relativos a zero, a forma centrada e `math.fsum` para reduzir erros de ponto flutuante. O conceito de cálculo numérico está no ajuste e no cuidado com estabilidade; não há promessa de solução exata em aritmética de máquina.

## Avaliação honesta

Os primeiros 80% (arredondados para baixo) são treino e o restante é teste, sem embaralhar. A comparação é com uma previsão ingênua: repetir em todo o teste a última venda do treino. O modelo final só usa todo o histórico depois da avaliação.

- MAE = Σ|y−ŷ|/n: erro absoluto médio, em unidades.
- RMSE = √[Σ(y−ŷ)²/n]: dá mais peso a erros grandes.
- R² = 1−Σ(y−ŷ)²/Σ(y−ȳ)²: pode ser negativo; indefinido para teste constante (JSON null).

As métricas usam previsões limitadas a zero, assim como as projeções. No exemplo n=36, treino=28, teste=8. Não escolha um modelo apenas pelo R² no treino. Uma tendência linear não representa sazonalidade, promoções, preços ou mudanças estruturais; o exemplo foi construído com tendência para demonstrar o algoritmo. Não há intervalo de previsão implementado.

## Saídas

`resultados/previsoes.csv`, `avaliacao.csv`, `metricas.json` e `grafico.svg` (abra no navegador). O gráfico distingue histórico, tendência e projeção; as métricas são calculadas sobre valores completos, antes de arredondar os CSVs.

## Como demonstrar e evoluir

1. Execute e explique o significado da inclinação mensal.
2. Abra `avaliacao.csv` e calcule um erro à mão.
3. Altere o histórico para uma série sem tendência e compare o baseline.
4. Evolução: validação temporal em múltiplas janelas, sazonalidade e intervalos de previsão.

Complexidade: ajuste e avaliação O(n), ordenação de entrada O(n log n), memória O(n+h), sendo h o horizonte.
