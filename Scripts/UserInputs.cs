using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Resphinx.Maze
{
    public class UserInputs
    {
        public class Input
        {
            public ButtonControl key;
            bool requested = false, checkDuplicate = false, lastFrame = false;
            public bool down = false, pressed = false;
            public Input(ButtonControl k, bool checkForDuplicate = false)
            {
                key = k;
                checkDuplicate = checkForDuplicate;
            }
            public void Update()
            {
                if (requested)
                {
                    requested = false;
                    down = key.isPressed;
                    if (checkDuplicate)
                        pressed = !lastFrame && key.wasPressedThisFrame;
                    else
                        pressed = key.wasPressedThisFrame;
                }
                else
                {
                    down |= key.isPressed;
                    if (checkDuplicate)
                        pressed = lastFrame || pressed || key.wasPressedThisFrame;
                    else
                        pressed |= key.wasPressedThisFrame;
                }
            }
            public void Check()
            {
                requested = true;
            }
          }
        static Input[] inputs;
    
        public const int Forward = 0;
        public const int Right = 1;
        public const int Left = 2;
        public const int Back = 4;
        public const int Pause = 3;
        public const int Dash = 5;
        public const int MLeft = 6;
        public const int MRight = 7;
        public const int Esc = 8;
        public const int Up = 9;
        public const int Down = 10;
        public const int View = 11;
        public const int Teleport = 12;


        public const int Count = 13;

        public static bool[] current = new bool[Count];
        public static void InitDefault()
        {
            inputs = new Input[Count];
            inputs[Forward] =new Input( Keyboard.current.wKey);
            inputs[Right] = new Input(Keyboard.current.dKey);
            inputs[Left] = new Input(Keyboard.current.aKey);
            inputs[Back] = new Input(Keyboard.current.sKey);
            inputs[Dash] = new Input(Keyboard.current.spaceKey);
            inputs[MLeft] = new Input(Mouse.current.leftButton,true);
            inputs[MRight] = new Input(Mouse.current.rightButton, true);
            inputs[Pause] = new Input(Keyboard.current.pKey, true);
            inputs[Esc] = new Input(Keyboard.current.escapeKey, true);
            inputs[Up] = new Input(Keyboard.current.qKey, true);
            inputs[Down] = new Input(Keyboard.current.eKey, true);
            inputs[View] = new Input(Keyboard.current.fKey, true);
            inputs[Teleport] = new Input(Keyboard.current.tKey, true);
        }
        public static void Update()
        {
            for(int i = 0; i < Count; i++)
                inputs[i].Update();
        }
        public static bool Hold(int i)
        {
            inputs[i].Check();
            return inputs[i].down;
        }
        public static bool Pressed(int i)
        {
             inputs[i].Check();
             return inputs[i].pressed;
        }       
    }
}
