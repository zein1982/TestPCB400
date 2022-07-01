using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;

namespace ComPort
{
    /// <summary>
    /// Интерфейс слушателя оконных сообщений, относящихся к портам
    /// </summary>
    public interface IWMListener
    {
        /// <summary>
        /// Закрыть порты
        /// </summary>
        void ClosePorts();
        /// <summary>
        /// Обновить список портов
        /// </summary>
        void RefreshPortList();
    }

    /// <summary>
    /// Класс, выполняющий "прослушку" и отправку оконных сообщений, для обнаружения и управления портами компьютера
    /// </summary>
    class WMController
    {
        /// <summary>
        /// Дескриптор окна
        /// </summary>
        private IntPtr hWnd;
        /// <summary>
        /// Код сообщения, предназначенного для освобождения портов
        /// </summary>
        private int CloseMessage;
        /// <summary>
        /// Ссылка на объект-слушатель оконных сообщений
        /// </summary>
        private LinkedList<IWMListener> listeners;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="wnd">Ссылка на главное окно программы</param>
        public WMController(MainWindow wnd)
        {
            listeners = new LinkedList<IWMListener>();

            // После загрузки главного окна добавляем метод WndProc в качестве "слушателя" оконных сообщений и регистрируем собственное сообщение
            wnd.Loaded += (sender, e) =>
            {
                hWnd = (new WindowInteropHelper(wnd)).Handle;
                HwndSource.FromHwnd(hWnd).AddHook(new HwndSourceHook(WndProc));
                CloseMessage = Win32.RegisterWindowMessage("WM_CLOSE_PORT");
            };
        }

        /// <summary>
        /// Отправить сообщение для освобождения портов, используемых другими программами
        /// </summary>
        public void ClosePorts()
        {
            Win32.PostMessage(new IntPtr(0xFFFF), CloseMessage, hWnd, IntPtr.Zero); // 0xFFFF - HWND_BROADCAST (сообщение для всех окон)
        }
        /// <summary>
        /// Зарегистрировать слушателя оконных сообщений
        /// </summary>
        /// <param name="listener">Ссылка на объект-слушатель</param>
        public void AddWMListener(IWMListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.AddLast(listener);
        }
        /// <summary>
        /// Удалить слушателя оконных сообщений
        /// </summary>
        /// <param name="listener">Ссылка на объект-слушатель, подлежащий удалению</param>
        public void RemoveWMListener(IWMListener listener)
        {
            listeners.Remove(listener);
        }
        /// <summary>
        /// Удалить все слушатели оконных сообщений
        /// </summary>
        public void RemoveAllWMListeners()
        {
            listeners.Clear();
        }

        /// <summary>
        /// Обработчик сообщений Windows
        /// </summary>
        /// <param name="hwnd">Дескриптор окна</param>
        /// <param name="msg">Код сообщения</param>
        /// <param name="wParam">Параметр сообщения</param>
        /// <param name="lParam">Дополнительный параметр сообщения</param>
        /// <param name="handled">Возвращаемый признак, идентифицирующий было ли обработано сообщение в данном обрабочике</param>
        /// <returns>В данном обработчике всегда возвращается нулевой уазатель</returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == 0x0219) //WM_DEVICECHANGE
            {
                handled = true;
                if ((wParam.ToInt32() == 0x8000)        // DBT_DEVICEARRIVAL
                    || (wParam.ToInt32() == 0x8004))    // DBT_DEVICEREMOVECOMPLETE
                {
                    foreach(IWMListener listener in listeners)
                        listener.RefreshPortList();
                }
            }
            else if(msg == CloseMessage)
            {
                handled = true;
                if(wParam != hWnd)
                    foreach(IWMListener listener in listeners)
                        listener.ClosePorts();
            }
            return IntPtr.Zero;
        }
    }
}
