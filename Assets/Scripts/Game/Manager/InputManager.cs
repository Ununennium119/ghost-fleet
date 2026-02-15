using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Common.Logger;

namespace Game.Manager {
    /// <summary>This class is responsible for game inputs.</summary>
    /// <remarks>This class is singleton</remarks>
    public class InputManager : MonoBehaviour {
        public static InputManager Instance { get; private set; }


        public event EventHandler<OnRotatePerformedArgs> OnRotatePerformed;
        public class OnRotatePerformedArgs : EventArgs {
            public float Value;
        }

        public event EventHandler OnCancelPerformed;


        private InputSystem_Actions _inputSystemActions;


        private void Awake() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);

            _inputSystemActions = new InputSystem_Actions();
            _inputSystemActions.Enable();

            _inputSystemActions.Player.Rotate.performed += RotatePerformed;
            _inputSystemActions.Player.Cancel.performed += CancelPerformed;
        }

        private void OnDestroy() {
            _inputSystemActions.Player.Rotate.performed -= RotatePerformed;

            _inputSystemActions.Dispose();
        }


        private void RotatePerformed(InputAction.CallbackContext context) {
            OnRotatePerformed?.Invoke(this, new OnRotatePerformedArgs { Value = context.ReadValue<float>() });
        }

        private void CancelPerformed(InputAction.CallbackContext context) {
            OnCancelPerformed?.Invoke(this, EventArgs.Empty);
        }
    }
}
