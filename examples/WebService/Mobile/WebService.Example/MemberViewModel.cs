using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Turbo.Maui.Services;
using Turbo.Maui.Services.Examples.Shared.Models;
using Turbo.Maui.Services.Models;

namespace WebService.Example
{
    public partial class MemberViewModel : ObservableObject, IQueryAttributable
    {
        public MemberViewModel(IWebService webService)
        {
            _WebService = webService;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query["member"].GetType() != typeof(ShortUser)) throw new ArgumentNullException();

            var member = (ShortUser)query["member"];
            if (member?.ID == null) throw new ArgumentNullException();
            GetMember(member.ID);
        }

        [RelayCommand]
        private void Edit()
        {
            if (Member == null) return;
            //cloning only data that the user can update.
            _OriginalMemberData = new()
            {
                AutoMapPinResults = Member.AutoMapPinResults,
                FirstName = Member.FirstName,
                LastName = Member.LastName,
                MeasurementStandard = Member.MeasurementStandard
            };
            InEditMode = true;
        }

        [RelayCommand]
        private async Task Update()
        {
            if (Member is null || _OriginalMemberData is null) throw new ArgumentNullException();

            var patch = new PatchDocument();

            if (Member.FirstName != _OriginalMemberData.FirstName)
                patch.Add(new(nameof(Member.FirstName), Member.FirstName));
            if (Member.LastName != _OriginalMemberData.LastName)
                patch.Add(new(nameof(Member.LastName), Member.LastName));
            if (Member.AutoMapPinResults != _OriginalMemberData.AutoMapPinResults)
                patch.Add(new(nameof(Member.AutoMapPinResults), Member.AutoMapPinResults));
            if (Member.MeasurementStandard != _OriginalMemberData.MeasurementStandard)
                patch.Add(new(nameof(Member.MeasurementStandard), Member.MeasurementStandard));

            if (patch.Count() == 0)//no changes made
            {
                InEditMode = false;
                return;
            }
            patch.Add(new(nameof(Member.UpdatedDate), DateTime.Now));
            patch.Add(new(nameof(Member.CreatedBy), "ClientUser"));


            //TODO time to get status responses back out to this level to be able to deal with errors.
            await _WebService.Put($"User/{Member.ID}", patch);
            InEditMode = false;
        }

        [RelayCommand]
        private void Cancel()
        {
            if (Member is null || _OriginalMemberData is null) throw new ArgumentNullException();
            Member.FirstName = _OriginalMemberData.FirstName;
            Member.LastName = _OriginalMemberData.LastName;
            Member.AutoMapPinResults = _OriginalMemberData.AutoMapPinResults;
            Member.MeasurementStandard = _OriginalMemberData.MeasurementStandard;
            InEditMode = false;
        }

        private async void GetMember(string id)
        {
            var response = await _WebService.Get<User>($"User/{id}");
            if (response == null || !response.WasSuccessful) return;
            Member = response.Data;
        }

        [ObservableProperty]
        private User? _Member;

        private User? _OriginalMemberData;

        [ObservableProperty]
        private bool _InEditMode;

        private readonly IWebService _WebService;
    }
}