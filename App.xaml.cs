using VnbisAgent.UI;

namespace VnbisAgent
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new PhoneTestPage());
        }
    }
}