namespace WebService.Example;

public partial class MemberPage : ContentPage
{
    public MemberPage(MemberViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
