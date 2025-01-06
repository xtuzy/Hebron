namespace Hebron.Roslyn
{
	public enum UnsafeArrayUsage
	{
		UseOrdinaryArray,
		UseUnsafeArray
	}

	public class RoslynConversionParameters: BaseConversionParameters
	{
		public UnsafeArrayUsage GlobalVariablesUnsafeArrayUsage;

		public string[] Args { get; set; }
        public string[] Classes { get; set; }

		public RoslynConversionParameters()
		{
			Classes = new string[0];
		}
	}
}
