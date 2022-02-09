using nn.hid;

public class SwitchInput
{
    public NpadId[] npadIds = { NpadId.Handheld, NpadId.No1, NpadId.No2 };
    public NpadState npadState = new NpadState();


    private static SwitchInput m_instance;
    private static object m_lock = new object();


    public static SwitchInput Instance
    {
        get
        {
            if (m_instance == null)
            {
                lock (m_lock)
                {
                    if (m_instance == null)
                    {
                        m_instance = new SwitchInput();
                    }
                }
            }

            return m_instance;
        }
    }


    private SwitchInput()
    {
        Npad.Initialize();
        Npad.SetSupportedIdType(npadIds);
        NpadJoy.SetHoldType(NpadJoyHoldType.Horizontal);

        Npad.SetSupportedStyleSet(NpadStyle.FullKey | NpadStyle.Handheld | NpadStyle.JoyDual | NpadStyle.JoyLeft | NpadStyle.JoyRight);

        for (int i = 1; i < npadIds.Length; i++)
        {
            NpadJoy.SetAssignmentModeSingle(npadIds[i]);
        }
    }
}
