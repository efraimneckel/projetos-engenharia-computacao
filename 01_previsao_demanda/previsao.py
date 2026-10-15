"""Regressão linear por mínimos quadrados, sem dependências externas."""
import argparse
import csv
import json
import math
from pathlib import Path
from datetime import date
from statistics import mean


def mes_indice(texto):
    data = date.fromisoformat(texto)
    if data.day != 1:
        raise ValueError("Use o primeiro dia de cada mês: AAAA-MM-01.")
    return data.year * 12 + data.month - 1


def data_mes(indice):
    ano, mes = divmod(indice, 12)
    return date(ano, mes + 1, 1).isoformat()


def carregar(caminho):
    with open(caminho, encoding="utf-8-sig", newline="") as arquivo:
        leitor = csv.DictReader(arquivo)
        if leitor.fieldnames != ["data", "vendas"]:
            raise ValueError("Cabeçalho esperado: data,vendas")
        dados = []
        for linha in leitor:
            indice = mes_indice(linha["data"])
            vendas = float(linha["vendas"])
            if not math.isfinite(vendas) or vendas < 0:
                raise ValueError("As vendas devem ser finitas e não negativas.")
            dados.append((indice, vendas))
    dados.sort()
    if len(dados) < 8:
        raise ValueError("Informe pelo menos oito meses para separar treino e teste.")
    if any(b[0] - a[0] != 1 for a, b in zip(dados, dados[1:])):
        raise ValueError("Há meses ausentes ou duplicados no histórico.")
    return dados


def ajustar(x, y):
    """Forma centrada reduz cancelamento numérico em relação às somas brutas."""
    if len(x) != len(y) or len(x) < 2:
        raise ValueError("O ajuste exige pelo menos dois pares de observações.")
    mx, my = mean(x), mean(y)
    sxx = math.fsum((v - mx) ** 2 for v in x)
    if sxx == 0:
        raise ValueError("Não há variação na variável independente.")
    b = math.fsum((xi - mx) * (yi - my) for xi, yi in zip(x, y)) / sxx
    return my - b * mx, b


def prever(modelo, x):
    a, b = modelo
    # Limite físico: não existe quantidade vendida negativa.
    return [max(0.0, a + b * xi) for xi in x]


def metricas(real, estimado):
    erros = [r - p for r, p in zip(real, estimado)]
    sse = math.fsum(e * e for e in erros)
    sst = math.fsum((r - mean(real)) ** 2 for r in real)
    return {
        "MAE": mean(abs(e) for e in erros),
        "RMSE": math.sqrt(sse / len(real)),
        "R2": 1 - sse / sst if sst > 0 else None,
    }


def grafico(caminho, dados, futuro, modelo):
    """SVG exportável: histórico, ajuste e previsão em coordenadas exatas."""
    w, h, margem = 900, 460, 65
    todos = dados + futuro
    max_x = max(x for x, _ in todos)
    max_y = max([y for _, y in todos] + prever(modelo, [max_x])) * 1.15 or 1
    def ponto(x, y):
        return f"{margem + x / max_x * (w-2*margem):.2f},{h-margem-y/max_y*(h-2*margem):.2f}"
    def linha(pares, cor, tracejado=""):
        pontos = " ".join(ponto(x, y) for x, y in pares)
        return f'<polyline points="{pontos}" fill="none" stroke="{cor}" stroke-width="3" {tracejado}/>'
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">',
           '<rect width="100%" height="100%" fill="#f8fafc"/>',
           '<g font-family="Arial" font-size="14" fill="#0f172a">',
           '<text x="65" y="28" font-size="21">Projeção mensal de vendas — dados demonstrativos</text>']
    for i in range(6):
        y = max_y * i / 5
        py = float(ponto(0, y).split(",")[1])
        svg += [f'<line x1="65" y1="{py}" x2="835" y2="{py}" stroke="#cbd5e1"/>',
                f'<text x="8" y="{py+5}">{y:.0f}</text>']
    svg += [linha(dados, "#2563eb"),
            linha([(0, prever(modelo, [0])[0]), (max_x, prever(modelo, [max_x])[0])], "#94a3b8", 'stroke-dasharray="5 5"'),
            linha([dados[-1]] + futuro, "#ea580c"),
            f'<text x="65" y="435">Mês relativo (0 = início do histórico); eixo vertical: unidades</text>',
            '<text x="65" y="55">Azul: histórico | Cinza: tendência ajustada | Laranja: projeção</text>']
    for x, _ in todos:
        if x % 3 == 0:
            px = ponto(x, 0).split(',')[0]
            svg.append(f'<text x="{px}" y="415">{x}</text>')
    svg += ['</g></svg>']
    caminho.write_text("\n".join(svg), encoding="utf-8")


def executar(entrada, saida, horizonte=6):
    if not 1 <= horizonte <= 120:
        raise ValueError("O horizonte deve estar entre 1 e 120 meses.")
    dados = carregar(entrada)
    origem = dados[0][0]
    x = [mes - origem for mes, _ in dados]
    y = [v for _, v in dados]
    corte = int(len(dados) * 0.8)
    # A separação é temporal: o futuro nunca participa do treinamento de avaliação.
    modelo_treino = ajustar(x[:corte], y[:corte])
    teste = prever(modelo_treino, x[corte:])
    referencia = [y[corte - 1]] * len(teste)
    avaliacao = metricas(y[corte:], teste)
    baseline = metricas(y[corte:], referencia)
    # Apenas depois de avaliar, todos os dados são usados para projetar o futuro.
    modelo_final = ajustar(x, y)
    xf = list(range(x[-1] + 1, x[-1] + horizonte + 1))
    yf = prever(modelo_final, xf)
    saida = Path(saida)
    saida.mkdir(parents=True, exist_ok=True)
    with (saida / "previsoes.csv").open("w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["data", "vendas_previstas"])
        writer.writerows((data_mes(origem + xi), f"{yi:.2f}") for xi, yi in zip(xf, yf))
    with (saida / "avaliacao.csv").open("w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["data", "real", "regressao", "baseline_ultimo_mes"])
        writer.writerows((data_mes(origem+xi), real, round(pred, 3), referencia[0])
                         for xi, real, pred in zip(x[corte:], y[corte:], teste))
    resumo = {"observacoes": len(dados), "treino": corte, "teste": len(teste),
              "regressao_teste": avaliacao, "baseline_teste": baseline,
              "modelo_final": {"intercepto": modelo_final[0], "inclinacao_mensal": modelo_final[1]},
              "dados": "Origem informada pelo usuário; amostra inclusa é sintética."}
    (saida / "metricas.json").write_text(json.dumps(resumo, indent=2, ensure_ascii=False), encoding="utf-8")
    grafico(saida / "grafico.svg", list(zip(x, y)), list(zip(xf, yf)), modelo_final)
    print(f"Treino: {corte} meses | Teste: {len(teste)} meses")
    print(f"MAE regressão: {avaliacao['MAE']:.2f} | MAE baseline: {baseline['MAE']:.2f}")
    print(f"Modelo final: vendas = {modelo_final[0]:.4f} + {modelo_final[1]:.4f} × mês")
    for xi, yi in zip(xf, yf):
        print(f"{data_mes(origem + xi)}: {yi:.2f} unidades")
    print(f"Arquivos gerados em: {saida.resolve()}")
    return resumo


if __name__ == "__main__":
    pasta = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--entrada", type=Path, default=pasta / "dados/vendas.csv")
    parser.add_argument("--saida", type=Path, default=pasta / "resultados")
    parser.add_argument("--meses", type=int, default=6)
    args = parser.parse_args()
    try:
        executar(args.entrada, args.saida, args.meses)
    except (ValueError, OSError, KeyError, csv.Error, OverflowError) as erro:
        parser.exit(1, f"Erro: {erro}\n")
