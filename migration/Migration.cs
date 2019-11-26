namespace migrator
{
        public class Migration
        {
            public Migration(string name, string query)
            {
                Name = name;
                Query = query;
            }

            public string Query { get; }

            public string Name { get; }
        }
}