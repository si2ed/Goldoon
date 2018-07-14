using Goldoon.WindowsService.Access;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace Goldoon.WindowsService
{
    partial class BotHandler : ServiceBase
    {
        public BackgroundWorker bw = new BackgroundWorker();
        private static readonly TelegramBotClient Bot = new TelegramBotClient(ConfigurationManager.AppSettings["APIKey"]);
        public bool _Die = false;



        public BotHandler()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(StartSystem);

            bw.RunWorkerAsync();



        }

        protected override void OnStop()
        {
            _Die = true;
            Thread.Sleep(3000);

            bw.CancelAsync();
        }


        private void StartSystem(object sender, DoWorkEventArgs e)
        {
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            //Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;
            Bot.OnMessage += BotOnMessageReceived;

            Bot.StartReceiving();


            System.Messaging.MessageQueue msgQ = new System.Messaging.MessageQueue(".\\Private$\\botshoppayment");


            EventLog.WriteEntry("BotHandler", "Queue intialized", EventLogEntryType.Warning, 240);


            Access.Queue_Payment pm_input = new Access.Queue_Payment();
            Object o = new Object();
            Type[] arrTypes = new Type[2];
            arrTypes[0] = pm_input.GetType();
            arrTypes[1] = o.GetType();
            msgQ.Formatter = new System.Messaging.XmlMessageFormatter(arrTypes);

            while (_Die == false)
            {
                try
                {
                    EventLog.WriteEntry("BotHandler", "Listener intialized", EventLogEntryType.Warning, 241);

                    pm_input = (Queue_Payment)msgQ.Receive().Body;

                    EventLog.WriteEntry("BotHandler", "Item receieved", EventLogEntryType.Warning, 242);

                    if (pm_input.Store != ConfigurationManager.AppSettings["StoreName"])
                    {
                        string sSource = "BotHandler";

                        EventLog.WriteEntry(sSource, "Item return to queue", EventLogEntryType.Warning, 234);

                        System.Messaging.Message msg = new System.Messaging.Message();
                        msg.Body = pm_input;
                        msgQ.Send(msg);
                        continue;
                    }
                    else
                    {
                        try
                        {
                            EventLog.WriteEntry("BotHandler", "Item handled", EventLogEntryType.Warning, 243);

                            using (Access.GoldoonEntities db = new Access.GoldoonEntities())
                            {
                                var _getOrder = db.Orders.Where(a => a.Id == pm_input.OrderId).FirstOrDefault();
                                if (_getOrder != null)
                                {
                                    _getOrder.Status = Logic.Utilities.EnumStatus.Paid;
                                    _getOrder.Paymetn_Refid = pm_input.RefId;

                                    db.SaveChanges();

                                    var _getCart = db.Carts.Where(a => a.User_Id == _getOrder.UserId).ToList();
                                    db.Carts.RemoveRange(_getCart);
                                    db.SaveChanges();

                                    var keyboard = new InlineKeyboardMarkup(new[]
                                             {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Logic.Utilities.Enum.Home.Text,Logic.Utilities.Enum.Home.Command)
                        } });

                                    var sb = new StringBuilder();
                                    sb.AppendLine("پرداخت انجام شده و سفارش شما ثبت شد، به همین زودیا بهتون خبر میدیم");
                                    sb.AppendLine("شماره پیگیری : " + pm_input.RefId);


                                    Bot.SendTextMessageAsync(_getOrder.User.Token, sb.ToString(),
                                                          replyMarkup: keyboard);


                                    string[] admin = ConfigurationManager.AppSettings["AdminToken"].Split('-');
                                    sb.Clear();
                                    sb.AppendLine("سفارش جدید");
                                    sb.AppendLine("@" + _getOrder.User.Username + "به نام " + _getOrder.User.Name);
                                    sb.AppendLine("سفارشات :");

                                    int TotalPrice = 0;
                                    foreach (var ord in _getOrder.OrderItems)
                                    {
                                        sb.AppendLine(ord.Product.Name + "-" + ord.Quantity);
                                        TotalPrice = TotalPrice + (ord.Product.Price.HasValue ? ord.Product.Price.Value : 0);
                                    }
                                    sb.AppendLine("مبلغ پرداخت شده :" + TotalPrice);
                                    sb.AppendLine("شماره پیگیری : " + pm_input.RefId);


                                    foreach (var item in admin)
                                    {
                                        Bot.SendTextMessageAsync(item, sb.ToString(),
                                                                                       replyMarkup: new ReplyKeyboardRemove());
                                    }



                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string sSource = "BotHandler";

                            EventLog.WriteEntry(sSource, ex.ToString(), EventLogEntryType.Warning, 236);

                        }
                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("BotHandler", ex.ToString(), EventLogEntryType.Warning, 295);

                }
            }
        }
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            //Debugger.Break();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        //private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        //{
        //    InlineQueryResult[] results = {
        //        new InlineQueryResultLocation
        //        {
        //            Id = "1",
        //            Latitude = 40.7058316f, // displayed result
        //            Longitude = -74.2581888f,
        //            Title = "New York",
        //            InputMessageContent = new InputLocationMessageContent // message if result is selected
        //            {
        //                Latitude = 40.7058316f,
        //                Longitude = -74.2581888f,
        //            }
        //        },

        //        new InlineQueryResultLocation
        //        {
        //            Id = "2",
        //            Longitude = 52.507629f, // displayed result
        //            Latitude = 13.1449577f,
        //            Title = "Berlin",
        //            InputMessageContent = new InputLocationMessageContent // message if result is selected
        //            {
        //                Longitude = 52.507629f,
        //                Latitude = 13.1449577f
        //            }
        //        }
        //    };

        //    await Bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
        //}

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;

                if (message != null)
                {
                    if (message.Type == MessageType.TextMessage && message.Text.StartsWith("/inline")) // send inline keyboard
                    {
                        await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                    new[] // first row
                    {
                         InlineKeyboardButton.WithCallbackData("1.1","/keyboard"),
                         InlineKeyboardButton.WithCallbackData("1.2"),
                    },
                    new[] // second row
                    {
                         InlineKeyboardButton.WithCallbackData("2.1"),
                         InlineKeyboardButton.WithCallbackData("2.2"),
                    }
                });

                        await Task.Delay(500); // simulate longer running task

                        await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                            replyMarkup: keyboard);
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text.StartsWith("/keyboard")) // send custom keyboard
                    {
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                    new [] // first row
                    {
                        new KeyboardButton("1.1"),
                        new KeyboardButton("1.2"),
                    },
                    new [] // last row
                    {
                        new KeyboardButton("2.1"),
                        new KeyboardButton("2.2"),
                    }
                });

                        await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                            replyMarkup: keyboard);
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text.StartsWith("/photo")) // send a photo
                    {
                        await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                        const string file = @"<FilePath>";

                        var fileName = file.Split('\\').Last();

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var fts = new FileToSend(fileName, fileStream);

                            await Bot.SendPhotoAsync(message.Chat.Id, fts, "Nice Picture");
                        }
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text.StartsWith("/request")) // request location or contact
                    {
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                    new KeyboardButton("Location")
                    {
                        RequestLocation = true
                    },
                    new KeyboardButton("Contact")
                    {
                        RequestContact = true
                    },
                });

                        await Bot.SendTextMessageAsync(message.Chat.Id, "Who or Where are you?", replyMarkup: keyboard);
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text.StartsWith("/start")) // Start bot
                    {
                        Logic.Method._CallbackQueryReciver.StartUser(Bot, message.Chat.Username, message.Chat.Id.ToString(), message.Chat.FirstName, message.Chat.LastName);
                        await Logic.Method._CallbackQueryReciver.HomePage(Bot, (int)message.Chat.Id);
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text == Logic.Utilities.Enum.Cart.Text)
                    {
                        await Logic.Method._CallbackQueryReciver.ViewCart(Bot, (int)message.Chat.Id);
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text == Logic.Utilities.Enum.CategoryList.Text)
                    {
                        await Logic.Method._CallbackQueryReciver.CategoryList(Bot, (int)message.Chat.Id);
                    }
                    else if (message.Type == MessageType.TextMessage && Logic.Utilities.Toolbelt.EnumCommandSplitter(message.Text)[0] == Logic.Utilities.Enum.DeleteCartItem.CommandBase)
                    {
                        await Logic.Method._CallbackQueryReciver.DeleteCartItem(Bot, (int)message.Chat.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(message.Text)[1]));
                    }
                    else if (message.Type == MessageType.TextMessage && Logic.Utilities.Toolbelt.EnumCommandSplitter(message.Text)[0] == Logic.Utilities.Enum.CancelOrder.CommandBase)
                    {
                        await Logic.Method._CallbackQueryReciver.CancelOrder(Bot, (int)message.Chat.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(message.Text)[1]));
                    }
                    else if (message.Type == MessageType.TextMessage && Logic.Utilities.Toolbelt.EnumCommandSplitter(message.Text)[0] == Logic.Utilities.Enum.CompleteOrder.CommandBase)
                    {
                        await Logic.Method._CallbackQueryReciver.CompleteOrder(Bot, (int)message.Chat.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(message.Text)[1]));
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text == Logic.Utilities.Enum.OpenOrders.Text)
                    {
                        await Logic.Method._CallbackQueryReciver.OpenOrders(Bot, (int)message.Chat.Id);
                    }
                    else if (message.Type == MessageType.TextMessage && message.Text == Logic.Utilities.Enum.AddProduct.Text)
                    {
                        await Logic.Method._CallbackQueryReciver.AddProduct(Bot, (int)message.Chat.Id);
                    }

                    else if (Logic.Utilities.Toolbelt.GetUserLastStatus(message.Chat.Id.ToString()) != Logic.Utilities.EnumLastActionStatus.None && message.Type == MessageType.TextMessage)
                        await Logic.Method._CallbackQueryReciver.CheckLastAction(Bot, message.Text, (int)message.Chat.Id);
                    else if (Logic.Utilities.Toolbelt.GetEditProductLastStatus(message.Chat.Id.ToString()) != Logic.Utilities.EnumLastActionStatus.None)
                    {
                        string FileId = string.Empty;
                        if (message.Document != null && message.Document.MimeType.Contains("image"))
                        {
                            FileId = message.Document.FileId;
                        }
                        else if (message.Photo != null)
                        {
                            FileId = message.Photo.FirstOrDefault().FileId;
                        }
                        await Logic.Method._CallbackQueryReciver.SaveEditProductAttribute(Bot, (int)message.Chat.Id, message.Type == MessageType.TextMessage ? message.Text : null, FileId);

                    }
                    else if (Logic.Utilities.Toolbelt.GetInsertProductStatus(message.Chat.Id.ToString()) == Logic.Utilities.EnumInsertModeStatus.Yes
                        && (message.Type == MessageType.DocumentMessage || message.Type == MessageType.PhotoMessage))
                        await Logic.Method._CallbackQueryReciver.SaveProduct(Bot, message);
                    else
                        await Logic.Method._CallbackQueryReciver.HomePage(Bot, (int)message.Chat.Id);
                }
            }
            catch (Exception ex)
            {
                string sSource = "BotHandler";

                EventLog.WriteEntry(sSource, ex.ToString(), EventLogEntryType.Warning, 294);
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            try
            {
                if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.Home.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.HomePage(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }

                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.Product.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.ProductList(Bot, callbackQueryEventArgs.CallbackQuery.From.Id, callbackQueryEventArgs.CallbackQuery.From.Id);
                }

                else if (Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data).FirstOrDefault() == Logic.Utilities.Enum.AddtoCart.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.AddtoCart(callbackQueryEventArgs, Bot, callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[1].ToString()));
                }

                else if (Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data).FirstOrDefault() == Logic.Utilities.Enum.AddtoCartWithAmount.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.AddtoCartWithAmount(callbackQueryEventArgs, Bot, callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[1].ToString()), callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[2].ToString()));
                }

                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.Cart.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.ViewCart(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }

                else if (Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data).FirstOrDefault() == Logic.Utilities.Enum.DeleteCartItem.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.DeleteCartItem(Bot, callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[1].ToString()));
                }
                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.ConfirmCart.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.ConfirmCart(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }
                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.CategoryList.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.CategoryList(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }

                else if (Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data).FirstOrDefault() == Logic.Utilities.Enum.CategoryProducts.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.CategoryProducts(Bot, callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[1].ToString()));
                }

                else if (Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data).FirstOrDefault() == Logic.Utilities.Enum.EditProduct.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.EditProduct(Bot, callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[1].ToString()), null);
                }
                else if (Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data).FirstOrDefault() == Logic.Utilities.Enum.EditProductAttributes.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.EditProductAttribute(Bot, callbackQueryEventArgs.CallbackQuery.From.Id, int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[1].ToString()), int.Parse(Logic.Utilities.Toolbelt.EnumCommandSplitter(callbackQueryEventArgs.CallbackQuery.Data)[2].ToString()));
                }
                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.CancelEditProduct.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.CancelEditProduct(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }
                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.PayOnDelivery.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.PayOnDelivery(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }
                else if (callbackQueryEventArgs.CallbackQuery.Data == Logic.Utilities.Enum.PayNow.CommandBase)
                {
                    await Logic.Method._CallbackQueryReciver.PayNow(Bot, callbackQueryEventArgs.CallbackQuery.From.Id);
                }
                //await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                //    $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
            }
            catch (Exception ex)
            {

                EventLog.WriteEntry("BotHandler", ex.ToString(), EventLogEntryType.Warning, 296);
            }
        }

    }
}
