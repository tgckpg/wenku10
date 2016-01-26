namespace wenku8.Model.Section
{
    class HistorySection : SearchableContext
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
