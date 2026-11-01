namespace MainMenu
{
    public class HomeUI : UI_Screen
    {
        public override void Back()
        {
            base.Back();
        }

        public override void ShowScreen()
        {
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void ShowCourses()
        {
            MainMenuUIManager.ShowUnitsUI();
        }

        public void ShowLoadGameUI()
        {
            MainMenuUIManager.ShowLoadGameUI();
        }

    }
}
