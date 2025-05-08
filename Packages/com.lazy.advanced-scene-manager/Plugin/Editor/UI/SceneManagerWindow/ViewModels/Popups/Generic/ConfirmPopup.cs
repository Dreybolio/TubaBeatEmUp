using System;
using AdvancedSceneManager.Editor.UI.Interfaces;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class ConfirmPopup : ViewModel, IPopup
    {

        bool isOpen => popupView.currentPopup == this;

        Action onConfirm;
        Action onCancel;
        string confirmText;
        string cancelText;
        string message;

        public override void OnReopen()
        {
            //It would be good to have the ability to call onCancel here, but this only happens after domain reload, so no shot.
            ClosePopup();
        }

        public void Prompt(Action onConfirm, Action onCancel = null, string confirmText = "OK", string cancelText = "Cancel", string message = "Are you sure?")
        {

            if (isOpen)
                throw new InvalidOperationException("Cannot display multiple prompts at a time.");

            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            this.confirmText = confirmText;
            this.cancelText = cancelText;
            this.message = message;

            OpenPopup<ConfirmPopup>();

        }

        public override void OnAdded()
        {

            var confirmButton = view.Q<Button>("button-confirm");
            var cancelButton = view.Q<Button>("button-cancel");

            confirmButton.text = confirmText;
            cancelButton.text = cancelText;

            cancelButton.clicked += onCancel;
            confirmButton.clicked += onConfirm;

            view.Q<Label>("label-message").text = message;

        }

    }

}
