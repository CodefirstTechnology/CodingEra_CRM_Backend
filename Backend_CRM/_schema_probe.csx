using Npgsql;
var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=CRM;User Id=postgres;Password=pravin@45;");
await conn.OpenAsync();
async Task Columns(string table) {
  await using var cmd = new NpgsqlCommand($"SELECT column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='{table}' ORDER BY ordinal_position", conn);
  await using var r = await cmd.ExecuteReaderAsync();
  Console.WriteLine($"=== {table} ===");
  while (await r.ReadAsync()) Console.WriteLine($"  {r.GetString(0)} ({r.GetString(1)})");
}
async Task Tables() {
  await using var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_name LIKE 'quotation%' ORDER BY 1", conn);
  await using var r = await cmd.ExecuteReaderAsync();
  Console.WriteLine("=== quotation tables ===");
  while (await r.ReadAsync()) Console.WriteLine($"  {r.GetString(0)}");
}
async Task History() {
  await using var cmd = new NpgsqlCommand("SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY 1", conn);
  await using var r = await cmd.ExecuteReaderAsync();
  Console.WriteLine("=== migration history (tail) ===");
  var all = new List<string>();
  while (await r.ReadAsync()) all.Add(r.GetString(0));
  foreach (var id in all.TakeLast(8)) Console.WriteLine($"  {id}");
  Console.WriteLine($"  ... total {all.Count}");
}
await Tables();
await Columns("quotations");
await Columns("quotation_line_items");
await Columns("organizations");
await Columns("deals");
await History();
