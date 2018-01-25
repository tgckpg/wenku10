namespace GR.Model.Section
{
	using static GR.History;

	class HistorySection : SearchableContext<HistoryItem>
	{
		private History History;

		public HistorySection()
		{
			History = new History();
		}

		public void Load()
		{
			SearchSet = History.GetListItems();
		}
	}

}