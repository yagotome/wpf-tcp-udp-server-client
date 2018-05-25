using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClienteTcpUdp
{
    enum State
    {
        ReadyToSend,
        Sending,
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
        private Socket sendingSocket;
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
                        string msg = TbMessage.Text;
                        new Thread(() => SendSocket(msg, ep, transportProtocol, out sendingSocket, HandleResponse, true)) { IsBackground = true }.Start();
                        State = State.Sending;
                        break;
                    case State.Sending:
                        sendingSocket?.Close();
                        State = State.ReadyToSend;
                        break;
                }
                TbMessage.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        void HandleResponse(string msgBack)
        {
            Dispatcher.Invoke(() =>
            {
                State = State.ReadyToSend;
                if (msgBack != null)
                    MessageBox.Show(msgBack);
            });
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
                        string message = ConsumeSocketBuffer(handler, buffer, protocol);
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

        static string ConsumeSocketBuffer(Socket socket, byte[] buffer, TransportProtocol protocol)
        {
            string message = "";
            int receivedBytes;
            DateTime i = DateTime.Now;
            while ((receivedBytes = socket.Receive(buffer)) > 0)
            {
                message += Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                if (protocol == TransportProtocol.UDP)
                    break;
                Console.WriteLine(DateTime.Now - i);
            }
            Console.WriteLine(DateTime.Now - i);
            return message;
        }

        /// <summary>
        /// Sends a segment by TCP or UDP socket, call a callback and optionally waits for response
        /// </summary>
        private static void SendSocket(string message, IPEndPoint endPoint, TransportProtocol protocol, out Socket socket, Action<string> callback, bool shouldWaitResponse = false)
        {
            using (socket = new Socket(endPoint.AddressFamily, protocol == TransportProtocol.TCP ? SocketType.Stream : SocketType.Dgram, ProtocolType.IP))
            {
                string msgBack = null;
                try
                {
                    socket.Connect(endPoint);
                    if (socket.Connected)
                    {
                        byte[] msgBytes = Encoding.UTF8.GetBytes(message);
                        socket.Send(msgBytes, msgBytes.Length, SocketFlags.None);
                        if (shouldWaitResponse)
                        {
                            byte[] rcvBuffer = new byte[1024];
                            msgBack = ConsumeSocketBuffer(socket, rcvBuffer, protocol);
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.ConnectionAborted)
                        MessageBox.Show($"Erro: {e.Message}");
                }
                callback?.Invoke(msgBack);
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
                    case State.Listening:
                        BtSendOrListen.Content = "Parar";
                        break;
                    case State.ReadyToSend:
                        BtSendOrListen.Content = "Enviar";
                        break;
                    case State.Sending:
                        BtSendOrListen.Content = "Cancelar";
                        break;
                }
                bool isntWaiting = state != State.Listening && state != State.Sending;
                TbIp.IsEnabled = isntWaiting;
                TbPort.IsEnabled = isntWaiting;
                CkbServidor.IsEnabled = isntWaiting;
                RdbTcp.IsEnabled = isntWaiting;
                RdbUdp.IsEnabled = isntWaiting;
                TbMessage.IsReadOnly = !isntWaiting;
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
