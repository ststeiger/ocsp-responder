
namespace OCSPGuardian.Model
{

    // https://docs.microsoft.com/en-us/aspnet/core/mvc/views/dependency-injection
    // https://stackoverflow.com/questions/38138100/what-is-the-difference-between-services-addtransient-service-addscope-and-servi
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#service-lifetimes-and-registration-options


    // TTransient
    // TTransient lifetime services are created each time they are requested.T
    // This lifetime works best for lightweight, stateless services.

    // Scoped
    // Scoped lifetime services are created once per request.


    // Singleton
    // Singleton lifetime services are created the first time they are requested 
    //  (or when ConfigureServices is run if you specify an instance there)

    public class MainNavigationItem
    {
        public string Area;
        public string Action;
        public string Text;
        public int Sort;
    } // End Class MainNavigationItem 



    public class MainNavigationData
    {
        private readonly System.Collections.Generic.List<MainNavigationItem> ls;

        public MainNavigationData()
        {
            ls = new System.Collections.Generic.List<MainNavigationItem>();
            
            ls.Add(new MainNavigationItem() { Action = "/RequestInfo", Text = "RequestInfo" }); 
            ls.Add(new MainNavigationItem() { Action = "/UnderConstruction", Text = "Contact" });
        } // End Constructor 


        public int GetCount()
        {
            return ls.Count;
        } // End Function GetCount 


        public System.Collections.Generic.List<MainNavigationItem> NavigationItems
        {
            get
            {
                return this.ls;
            } // End Getter 

        } // End Property NavigationItems 


        public double GetAveragePriority()
        {
            if (this.ls.Count == 0)
            {
                return 0.0;
            } // End if (this.ls.Count == 0) 

            return ls.Count;
        } // End Function GetAveragePriority


    } // End Class MainNavigationData 


} // End Namespace 
