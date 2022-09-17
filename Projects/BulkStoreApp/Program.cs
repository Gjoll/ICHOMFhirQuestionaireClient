namespace BulkStoreApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    throw new Exception($"Expect single arg, directory path");
                BulkStore bs = new BulkStore();
                bs.StoreAzureHealthCare(args[0]);
                //bs.StoreFirely(args[0]);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
    }
}