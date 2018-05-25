using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ClienteTcpUdp
{
    enum State
    {
        ReadyToSend,
        Listening,
        ReadyToListen
    }
    enum TransportProtocol
    {
        TCP,
        UDP
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private State state;
        private Socket listeningSocket;
        private TransportProtocol transportProtocol;

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
                string ip = TbIp.Text;
                int port = int.Parse(TbPort.Text);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                switch (State)
                {
                    case State.Listening:
                        listeningSocket?.Close();
                        State = State.ReadyToListen;
                        break;
                    case State.ReadyToListen:
                        new Thread(() => ListenSocket(UpdateTbMessage, ep, transportProtocol, out listeningSocket)) { IsBackground = true }.Start();
                        State = State.Listening;
                        break;
                    case State.ReadyToSend:
                        SendSocket(TbMessage.Text, ep, transportProtocol);
                        break;
                }
                TbMessage.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void UpdateTbMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TbMessage.Text += message;
                TbMessage.ScrollToEnd();
            });
        }

        private static void ListenSocket(Action<string> callback, IPEndPoint endPoint, TransportProtocol protocol, out Socket listener)
        {
            byte[] buffer = new byte[1024];
            using (listener = new Socket(endPoint.AddressFamily, protocol == TransportProtocol.TCP ? SocketType.Stream : SocketType.Dgram, ProtocolType.IP))
            {
                try
                {
                    listener.Bind(endPoint);
                    if (protocol == TransportProtocol.TCP)
                        listener.Listen(200);

                    while (true)
                    {
                        Socket handler = protocol == TransportProtocol.TCP ? listener.Accept() : listener;
                        string message = "";
                        int receivedBytes;
                        while ((receivedBytes = handler.Receive(buffer)) > 0)
                        {
                            message += Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                            if (protocol == TransportProtocol.UDP)
                                break;
                        }
                        callback(message);
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.Interrupted)
                        MessageBox.Show($"Erro: {e.Message}");
                }
            }
        }

        private void SendSocket(string message, IPEndPoint endPoint, TransportProtocol protocol)
        {
            using (Socket socket = new Socket(endPoint.AddressFamily, protocol == TransportProtocol.TCP ? SocketType.Stream : SocketType.Dgram, ProtocolType.IP))
            {
                socket.Connect(endPoint);
                if (socket.Connected)
                {
                    byte[] msgBytes = Encoding.UTF8.GetBytes(message);
                    socket.Send(msgBytes, msgBytes.Length, SocketFlags.None);
                }
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
                RdbTcp.IsEnabled = isntListening;
                RdbUdp.IsEnabled = isntListening;
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

        private void RdbUdp_Checked(object sender, RoutedEventArgs e)
        {
            transportProtocol = TransportProtocol.UDP;
        }

        private void RdbTcp_Checked(object sender, RoutedEventArgs e)
        {
            transportProtocol = TransportProtocol.TCP;
        }
    }
}
