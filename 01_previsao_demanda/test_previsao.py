import tempfile
import unittest
from pathlib import Path
from previsao import ajustar, prever, carregar, executar, metricas

class RegressaoTests(unittest.TestCase):
    def test_reta_conhecida(self):
        a, b = ajustar([0, 1, 2, 3], [3, 5, 7, 9])
        self.assertAlmostEqual(a, 3)
        self.assertAlmostEqual(b, 2)
        self.assertEqual(prever((a, b), [4]), [11])

    def test_degeneracao_e_limite(self):
        with self.assertRaises(ValueError): ajustar([1, 1], [2, 3])
        self.assertEqual(prever((1, -2), [3]), [0.0])
        self.assertIsNone(metricas([2, 2], [2, 2])["R2"])

    def test_pipeline_e_mes_faltando(self):
        with tempfile.TemporaryDirectory() as d:
            resumo = executar(Path(__file__).parent / 'dados/vendas.csv', d)
            self.assertEqual(resumo['treino'], 28)
            self.assertEqual(resumo['teste'], 8)
            self.assertLess(resumo['regressao_teste']['MAE'], resumo['baseline_teste']['MAE'])
            self.assertTrue((Path(d)/'grafico.svg').exists())
            p = Path(d)/'invalido.csv'
            p.write_text('data,vendas\n'+'\n'.join(f'2024-{i:02d}-01,10' for i in [1,2,3,4,5,6,7,9]))
            with self.assertRaises(ValueError): carregar(p)

if __name__ == '__main__': unittest.main()
