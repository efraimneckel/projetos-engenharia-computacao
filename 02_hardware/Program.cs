using System.Globalization;
using System.Text.Json;

// As notas, preços e peças do catálogo são didáticos e fictícios.
public record Cpu(string Name, decimal Price, string Socket, string MemoryType, int Watts, double Score);
public record Board(string Name, decimal Price, string Socket, string MemoryType, string Form, int MaxRamGb, bool M2Nvme);
public record Ram(string Name, decimal Price, string MemoryType, int Gb);
public record Gpu(string Name, decimal Price, int Watts, int LengthMm, int Pcie8Pin, double Score);
public record Psu(string Name, decimal Price, int Watts, int Pcie8Pin);
public record Ssd(string Name, decimal Price, int Gb);
public record Case(string Name, decimal Price, string[] Forms, int MaxGpuMm);
public record Catalog(Cpu[] Cpus, Board[] Boards, Ram[] Rams, Gpu[] Gpus, Psu[] Psus, Ssd[] Ssds, Case[] Cases);
public record Build(Cpu Cpu, Board Board, Ram Ram, Gpu Gpu, Psu Psu, Ssd Ssd, Case Case)
{
    public decimal Price => Cpu.Price + Board.Price + Ram.Price + Gpu.Price + Psu.Price + Ssd.Price + Case.Price;
    // Reserva fixa de 70 W para placa, RAM, SSD, ventoinhas e periféricos internos.
    public int EstimatedWatts => Cpu.Watts + Gpu.Watts + 70;
    public double Score => 0.4 * Cpu.Score + 0.6 * Gpu.Score;
    public double Value => Score / (double)Price * 1000;
}

public static class Program
{
    public static string[] Incompatibilities(Build b)
    {
        var reasons = new List<string>();
        if (b.Cpu.Socket != b.Board.Socket) reasons.Add("Socket de CPU e placa-mãe diferentes");
        if (b.Cpu.MemoryType != b.Ram.MemoryType || b.Board.MemoryType != b.Ram.MemoryType)
            reasons.Add("Geração de memória incompatível com CPU ou placa-mãe");
        if (b.Ram.Gb > b.Board.MaxRamGb) reasons.Add("RAM acima da capacidade da placa-mãe");
        if (!b.Board.M2Nvme) reasons.Add("Placa-mãe sem suporte ao SSD M.2 NVMe do catálogo");
        if (!b.Case.Forms.Contains(b.Board.Form)) reasons.Add("Formato da placa-mãe não cabe no gabinete");
        if (b.Gpu.LengthMm > b.Case.MaxGpuMm) reasons.Add("GPU longa demais para o gabinete");
        if (b.Psu.Pcie8Pin < b.Gpu.Pcie8Pin) reasons.Add("Quantidade insuficiente de conectores PCIe de 8 pinos");
        if (b.Psu.Watts < Math.Ceiling(b.EstimatedWatts * 1.25)) reasons.Add("Fonte abaixo da margem de potência de 25%");
        return reasons.ToArray();
    }

    public static IEnumerable<Build> Enumerate(Catalog c)
    {
        // Produto cartesiano: uma escolha de cada categoria, sem repetir categoria.
        foreach (var cpu in c.Cpus)
        foreach (var board in c.Boards)
        foreach (var ram in c.Rams)
        foreach (var gpu in c.Gpus)
        foreach (var psu in c.Psus)
        foreach (var ssd in c.Ssds)
        foreach (var box in c.Cases)
            yield return new Build(cpu, board, ram, gpu, psu, ssd, box);
    }

    public static bool Valid(Catalog c)
    {
        if (c.Cpus is not { Length: > 0 } || c.Boards is not { Length: > 0 } ||
            c.Rams is not { Length: > 0 } || c.Gpus is not { Length: > 0 } ||
            c.Psus is not { Length: > 0 } || c.Ssds is not { Length: > 0 } || c.Cases is not { Length: > 0 }) return false;
        bool Text(string? x) => !string.IsNullOrWhiteSpace(x);
        return c.Cpus.All(x => x is not null && Text(x.Name) && x.Price > 0 && Text(x.Socket) && Text(x.MemoryType) && x.Watts > 0 && double.IsFinite(x.Score) && x.Score > 0 && x.Score <= 100) &&
            c.Boards.All(x => x is not null && Text(x.Name) && x.Price > 0 && Text(x.Socket) && Text(x.MemoryType) && Text(x.Form) && x.MaxRamGb > 0) &&
            c.Rams.All(x => x is not null && Text(x.Name) && x.Price > 0 && Text(x.MemoryType) && x.Gb > 0) &&
            c.Gpus.All(x => x is not null && Text(x.Name) && x.Price > 0 && x.Watts > 0 && x.LengthMm > 0 && x.Pcie8Pin >= 0 && double.IsFinite(x.Score) && x.Score > 0 && x.Score <= 100) &&
            c.Psus.All(x => x is not null && Text(x.Name) && x.Price > 0 && x.Watts > 0 && x.Pcie8Pin >= 0) &&
            c.Ssds.All(x => x is not null && Text(x.Name) && x.Price > 0 && x.Gb > 0) &&
            c.Cases.All(x => x is not null && Text(x.Name) && x.Price > 0 && x.Forms is { Length: > 0 } && x.Forms.All(Text) && x.MaxGpuMm > 0);
    }

    public static void SelfTest(Catalog c)
    {
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        var all = Enumerate(c).ToList();
        var compatible = all.Where(b => Incompatibilities(b).Length == 0).ToList();
        Check(all.Count == c.Cpus.Length * c.Boards.Length * c.Rams.Length * c.Gpus.Length * c.Psus.Length * c.Ssds.Length * c.Cases.Length, "Produto cartesiano incorreto");
        Check(compatible.Count > 0, "Nenhuma montagem válida");
        var b = compatible[0];
        Check(Incompatibilities(b with { Board = b.Board with { Socket = "INVALIDO" } }).Length > 0, "Socket não validado");
        Check(Incompatibilities(b with { Ram = b.Ram with { MemoryType = "INVALIDA" } }).Length > 0, "DDR não validado");
        Check(Incompatibilities(b with { Psu = b.Psu with { Watts = 1 } }).Length > 0, "Fonte não validada");
        Check(Incompatibilities(b with { Gpu = b.Gpu with { LengthMm = 9999 } }).Length > 0, "Tamanho da GPU não validado");
        Check(Incompatibilities(b with { Board = b.Board with { M2Nvme = false } }).Length > 0, "SSD não validado");
        Check(Incompatibilities(b with { Ram = b.Ram with { Gb = 9999 } }).Length > 0, "Capacidade de RAM não validada");
        Check(Incompatibilities(b with { Case = b.Case with { Forms = new[] { "INVALIDO" } } }).Length > 0, "Formato não validado");
        Check(Incompatibilities(b with { Gpu = b.Gpu with { Pcie8Pin = 99 } }).Length > 0, "Conectores não validados");
        Console.WriteLine($"Testes aprovados: {all.Count} combinações, {compatible.Count} compatíveis.");
    }

    public static int Main(string[] args)
    {
        try
        {
            decimal budget = 4000;
            int top = 3, minRam = 16, minSsd = 500;
            string mode = "valor", catalogPath = Path.Combine(AppContext.BaseDirectory, "catalogo.json");
            bool test = false;
            for (int i = 0; i < args.Length; i++)
            {
                string Next() => ++i < args.Length ? args[i] : throw new ArgumentException("Falta valor para a opção.");
                switch (args[i])
                {
                    case "--orcamento": budget = decimal.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--top": top = int.Parse(Next()); break;
                    case "--ram": minRam = int.Parse(Next()); break;
                    case "--ssd": minSsd = int.Parse(Next()); break;
                    case "--modo": mode = Next(); break;
                    case "--catalogo": catalogPath = Next(); break;
                    case "--self-test": test = true; break;
                    case "--help":
                        Console.WriteLine("dotnet run -- --orcamento 4000 --ram 16 --ssd 500 --top 3 --modo valor|desempenho [--catalogo caminho] [--self-test]");
                        return 0;
                    default: throw new ArgumentException($"Opção desconhecida: {args[i]}");
                }
            }
            if (budget <= 0 || top < 1 || minRam < 1 || minSsd < 1 || (mode != "valor" && mode != "desempenho"))
                throw new ArgumentException("Valores inválidos. Use --help.");
            var c = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(catalogPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (c is null || !Valid(c)) throw new ArgumentException("Catálogo inválido ou incompleto.");
            if (test) { SelfTest(c); return 0; }
            var all = Enumerate(c).ToList();
            var technicallyValid = all.Where(b => Incompatibilities(b).Length == 0).ToList();
            var feasible = technicallyValid.Where(b => b.Price <= budget && b.Ram.Gb >= minRam && b.Ssd.Gb >= minSsd).ToList();
            var ranked = mode == "valor"
                ? feasible.OrderByDescending(b => b.Value).ThenByDescending(b => b.Score).ThenBy(b => b.Price)
                : feasible.OrderByDescending(b => b.Score).ThenBy(b => b.Price);
            Console.WriteLine("CATÁLOGO DIDÁTICO: preços fictícios em reais e notas simuladas.\n");
            Console.WriteLine($"Combinações: {all.Count}; compatíveis: {technicallyValid.Count}; atendem aos filtros: {feasible.Count}");
            if (feasible.Count == 0) { Console.WriteLine("Nenhuma montagem atende. Ajuste os filtros ou o catálogo."); return 0; }
            int index = 0;
            foreach (var b in ranked.Take(top))
            {
                Console.WriteLine($"\n#{++index}: R$ {b.Price.ToString("F2", CultureInfo.InvariantCulture)} | Nota: {b.Score:F1} | Nota/R$ 1.000: {b.Value:F2}");
                Console.WriteLine($"CPU: {b.Cpu.Name}\nPlaca-mãe: {b.Board.Name}\nRAM: {b.Ram.Name}\nGPU: {b.Gpu.Name}\nFonte: {b.Psu.Name}\nSSD: {b.Ssd.Name}\nGabinete: {b.Case.Name}");
                Console.WriteLine($"Carga estimada: {b.EstimatedWatts} W | Mínimo com margem: {Math.Ceiling(b.EstimatedWatts * 1.25)} W");
            }
            return 0;
        }
        catch (Exception ex) when (ex is IOException or JsonException or ArgumentException or OverflowException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Erro: {ex.Message}");
            return 1;
        }
    }
}
