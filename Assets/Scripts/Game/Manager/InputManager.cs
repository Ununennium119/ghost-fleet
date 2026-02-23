using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Common.Utility.Logger;

namespace Game.Manager {
    /// <remarks>This class is singleton</remarks>
    public class InputManager : MonoBehaviour {
        public static InputManager Instance { get; private set; }


        public event EventHandler OnClickPerformed;

        public event EventHandler<OnRotatePerformedArgs> OnRotatePerformed;
        public class OnRotatePerformedArgs : EventArgs {
            public float Value;
        }

        public event EventHandler OnCancelPerformed;


        private InputSystem_Actions _inputSystemActions;


        private void Awake() {
            InitializeSingleton();
            InitializeInputSystemActions();
            SubscribeToEvents();
        }

        private void OnDestroy() {
            UnsubscribeFromEvents();
            _inputSystemActions.Dispose();
        }


        private void InitializeSingleton() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);
        }

        private void InitializeInputSystemActions() {
            _inputSystemActions = new InputSystem_Actions();
            _inputSystemActions.Enable();
        }

        private void SubscribeToEvents() {
            _inputSystemActions.Player.Click.performed += ClickPerformed;
            _inputSystemActions.Player.Rotate.performed += RotatePerformed;
            _inputSystemActions.Player.Cancel.performed += CancelPerformed;
        }

        private void UnsubscribeFromEvents() {
            _inputSystemActions.Player.Click.performed -= ClickPerformed;
            _inputSystemActions.Player.Rotate.performed -= RotatePerformed;
            _inputSystemActions.Player.Cancel.performed -= CancelPerformed;
        }


        private void ClickPerformed(InputAction.CallbackContext context) {
            OnClickPerformed?.Invoke(this, EventArgs.Empty);
        }

        private void RotatePerformed(InputAction.CallbackContext context) {
            OnRotatePerformed?.Invoke(this, new OnRotatePerformedArgs { Value = context.ReadValue<float>() });
        }

        private void CancelPerformed(InputAction.CallbackContext context) {
            OnCancelPerformed?.Invoke(this, EventArgs.Empty);
        }
    }
}
