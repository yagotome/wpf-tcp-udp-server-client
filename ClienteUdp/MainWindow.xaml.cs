using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ClienteUdp
{
    enum State
    {
        ReadyToSend,
        Listening,
        ReadyToListen
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private State state;
        private Thread listeningThread;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtSendOrListen_Click(object sender, RoutedEventArgs e)
        {
            SendOrListenAction();
        }

        private void SendOrListenAction()
        {
            try
            {
                switch (State)
                {
                    case State.Listening:
                        listeningThread.Abort();
                        listeningThread = null;
                        State = State.ReadyToListen;
                        break;
                    case State.ReadyToListen:
                        string ip = TbIp.Text;
                        int port = int.Parse(TbPort.Text);
                        listeningThread = new Thread(() =>
                        {
                            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                            using (UdpClient udpClient = new UdpClient(ep))
                            {
                                while (true)
                                {
                                    byte[] data = udpClient.Receive(ref ep);
                                    string msg = Encoding.UTF8.GetString(data);
                                    Dispatcher.Invoke(() =>
                                    {
                                        TbMessage.Text += msg;
                                        TbMessage.ScrollToEnd();
                                    });
                                }
                            }

                        })
                        { IsBackground = true };
                        listeningThread.Start();
                        State = State.Listening;
                        break;
                    case State.ReadyToSend:
                        using (UdpClient udpClient = new UdpClient(TbIp.Text, int.Parse(TbPort.Text)))
                        {
                            Byte[] sendBytes = Encoding.UTF8.GetBytes(TbMessage.Text);
                            udpClient.Send(sendBytes, sendBytes.Length);
                        }
                        break;
                }
                TbMessage.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void CkbServidor_Click(object sender, RoutedEventArgs e)
        {
            State = (CkbServidor.IsChecked ?? false) ? State.ReadyToListen : State.ReadyToSend;
        }

        private State State
        {
            get => state;
            set
            {
                state = value;
                switch (state)
                {
                    case State.ReadyToListen:
                        BtSendOrListen.Content = "Ouvir";
                        break;
                    case State.ReadyToSend:
                        BtSendOrListen.Content = "Enviar";
                        break;
                    case State.Listening:
                        BtSendOrListen.Content = "Parar";
                        break;
                }
                bool isntListening = state != State.Listening;
                TbIp.IsEnabled = isntListening;
                TbPort.IsEnabled = isntListening;
                CkbServidor.IsEnabled = isntListening;
                TbMessage.IsReadOnly = !isntListening;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                SendOrListenAction();
            }
        }
    }
}
