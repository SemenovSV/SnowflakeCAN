namespace MySpace
{
    class Study
    {
        delegate int Summ(int Number);//создание ссылки на метод, делегата
        static Summ SomeVar()
        {
            int result = 0;
            Summ del = delegate (int number)//анонимный метод 
            {
                for (int i = 0; i < number; i++)
                {
                    result += i;
                }
                return result;
            };
            return del;
        }
        void doIt()
        {
            Summ del1 = SomeVar();
            
        }
    }
}
