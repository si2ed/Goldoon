using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Goldoon.WindowsService.Access;
using static Goldoon.Logic.Utilities.Toolbelt;
using System.Net;
using Newtonsoft.Json;

namespace Goldoon.Logic.Method
{
    class _CallbackQueryReciver
    {
        public static async void StartUser(TelegramBotClient Bot, string Username, string Token, string Name, string Family)
        {
            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _checkUser = db.Users.Where(a => a.Token == Token).FirstOrDefault();
                if (_checkUser == null)
                {
                    WindowsService.Access.User u = new WindowsService.Access.User()
                    {
                        Family = Family,
                        Name = Name,
                        Start_Date = DateTime.Now,
                        Token = Token,
                        Username = Username
                    };
                    db.Users.Add(u);
                    db.SaveChanges();
                }
            }



            var sb = new StringBuilder();
            sb.AppendLine(string.Format("خوش اومدی {0}", Name));


            await Bot.SendTextMessageAsync(Token, sb.ToString(),
replyMarkup: new ReplyKeyboardRemove());

        }
        public static async Task HomePage(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            string[] admin = ConfigurationManager.AppSettings["AdminToken"].Split('-');
            if (admin.Any(a => a == ChatId.ToString()))
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                         {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                    },
                      new []
                    {
                          new KeyboardButton(Utilities.Enum.OpenOrders.Text),
                          new KeyboardButton(Utilities.Enum.AddProduct.Text)
                    }
            });
                keyboard.ResizeKeyboard = true;

                await Bot.SendTextMessageAsync(ChatId, "در خدمتتون هستم",
                    replyMarkup: keyboard);
            }
            else
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text)
                    }
            });
                keyboard.ResizeKeyboard = true;

                await Bot.SendTextMessageAsync(ChatId, "در خدمتتون هستم",
              
            }
        }
        public static async Task CheckLastAction(TelegramBotClient Bot, string text, int ChatId)
        {
            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _checkUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_checkUser != null)
                {
                    if (_checkUser.Last_Info_Status == Utilities.EnumLastActionStatus.WaitForAddress)
                    {
                        _checkUser.Address = text;
                        db.SaveChanges();

                        await ConfirmCart(Bot, ChatId);

                    }
                    else if (_checkUser.Last_Info_Status == Utilities.EnumLastActionStatus.WaitForMobile)
                    {
                        if (text.Length < 10)
                        {
                            await Bot.SendTextMessageAsync(ChatId, "به نظر شماره ات درست نیس، دقت کن لطفا !",
                                                replyMarkup: new ReplyKeyboardRemove());
                        }
                        else
                        {
                            _checkUser.Mobile = text;
                            _checkUser.Last_Info_Status = Utilities.EnumLastActionStatus.None;

                            db.SaveChanges();

                            await ConfirmCart(Bot, ChatId);
                        }
                    }
                }
            }
        }
        public static async Task ProductList(TelegramBotClient Bot, int ChatId, int UserToken)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.UploadPhoto);

            const string BaseAddress = @"C:\inetpub\vhosts\si2ed.ir\payment.si2ed.ir\WindowsService\Images\";

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _products = db.Products.Where(a => a.isActive == true && a.itemsAvailable > 0 && !a.Carts.Any(b => b.User.Token == UserToken.ToString()));
                if (!_products.Any())
                {
                    await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new []
                        {
                            new KeyboardButton(Utilities.Enum.Product.Text),
                            new KeyboardButton(Utilities.Enum.Cart.Text),
                            new KeyboardButton(Utilities.Enum.Home.Text),
                        }
                    });

                    await Bot.SendTextMessageAsync(ChatId, "شرمنده، محصولی نمونده بدیم خدمتتون. اگر چیزی توی سبد خریدت هست، میتونی سفارش ات رو نهایی کنی",
                        replyMarkup: keyboard);
                }
                else
                {
                    foreach (var item in _products)
                    {
                        InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup();
                        string[] admin = ConfigurationManager.AppSettings["AdminToken"].Split('-');
                        if (admin.Any(a => a == ChatId.ToString()))
                        {
                            keyboard = new InlineKeyboardMarkup(new[]
                                                    {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.AddtoCart.Text,String.Format( Utilities.Enum.AddtoCart.Command,item.Id)),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Cart.Text,Utilities.Enum.Cart.Command),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Home.Text,Utilities.Enum.Home.Command)
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.EditProduct.Text,String.Format( Utilities.Enum.EditProduct.Command,item.Id))
                                }
                            });
                        }
                        else
                        {
                            keyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.AddtoCart.Text,String.Format( Utilities.Enum.AddtoCart.Command,item.Id)),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Cart.Text,Utilities.Enum.Cart.Command),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Home.Text,Utilities.Enum.Home.Command)
                                }
                            });

                        }
                        //image file
                        string file = BaseAddress + item.Image;
                        var fileName = file.Split('\\').Last();

                        //caption
                        var sb = new StringBuilder();
                        sb.AppendLine("نام: " + item.Name);
                        sb.Append(Environment.NewLine);
                        sb.AppendLine("توضیحات: " + item.Description);
                        sb.Append(Environment.NewLine);
                        sb.AppendLine("قیمت: " + item.Price + "تومان");

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var fts = new FileToSend(fileName, fileStream);

                            await Bot.SendPhotoAsync(ChatId, fts, sb.ToString(), false, 0, keyboard);
                        }
                    }
                    await Bot.SendTextMessageAsync(ChatId, "محصول دلخواه خود را انتخاب کنید",
                 replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }
        public static async Task Product(TelegramBotClient Bot, int ChatId, int ProductId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.UploadPhoto);

            const string BaseAddress = @"C:\inetpub\vhosts\si2ed.ir\payment.si2ed.ir\WindowsService\Images\";

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _products = db.Products.Find(ProductId);
                if (_products != null)
                {
                    //image file
                    string file = BaseAddress + _products.Image;
                    var fileName = file.Split('\\').Last();

                    //caption
                    var sb = new StringBuilder();
                    sb.Append("نام: " + _products.Name.Replace("\r\n", ""));
                    sb.Append(Environment.NewLine);
                    sb.Append("قیمت: " + string.Format("{0:n0}", _products.Price).Replace("\r\n", "") + " تومان ");
                    sb.Append(Environment.NewLine);
                    sb.Append("دسته بندی: " + _products.Category.Name.Replace("\r\n", ""));
                    sb.Append(Environment.NewLine);
                    sb.Append("موجودی انبار: " + _products.itemsAvailable);

                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var fts = new FileToSend(fileName, fileStream);

                        await Bot.SendPhotoAsync(ChatId, fts, sb.ToString(), false, 0, new ReplyKeyboardRemove());
                    }
                }
            }
        }
        public static async Task AddtoCart(CallbackQueryEventArgs callbackQueryEventArgs, TelegramBotClient Bot, int ChatId, int ProductId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("1",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"1")),
                            InlineKeyboardButton.WithCallbackData("2",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"2")),
                            InlineKeyboardButton.WithCallbackData("3",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"3")),
                            InlineKeyboardButton.WithCallbackData("4",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"4"))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("5",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"5")),
                            InlineKeyboardButton.WithCallbackData("6",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"6")),
                            InlineKeyboardButton.WithCallbackData("7",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"7")),
                            InlineKeyboardButton.WithCallbackData("8",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"8"))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("9",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"9")),
                            InlineKeyboardButton.WithCallbackData("10",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"10")),
                            InlineKeyboardButton.WithCallbackData("11",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"11")),
                            InlineKeyboardButton.WithCallbackData("12",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"12"))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("13",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"13")),
                            InlineKeyboardButton.WithCallbackData("14",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"14")),
                            InlineKeyboardButton.WithCallbackData("15",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"15")),
                            InlineKeyboardButton.WithCallbackData("16",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"16"))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("17",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"17")),
                            InlineKeyboardButton.WithCallbackData("18",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"18")),
                            InlineKeyboardButton.WithCallbackData("19",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"19")),
                            InlineKeyboardButton.WithCallbackData("20",String.Format( Utilities.Enum.AddtoCartWithAmount.Command,ProductId,"20"))
                        },
                    });

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getProduct = db.Products.Find(ProductId);
                if (_getProduct != null)
                    await Bot.SendTextMessageAsync(ChatId, "چند تا " + _getProduct.Name + " میخوای؟",
                      replyMarkup: keyboard);
            }

        }
        public static async Task AddtoCartWithAmount(CallbackQueryEventArgs callbackQueryEventArgs, TelegramBotClient Bot, int ChatId, int ProductId, int UserToken, int Amount)
        {
            using (GoldoonEntities db = new GoldoonEntities())
            {
                bool Proceed = true;

                var _getUser = db.Users.Where(a => a.Token == UserToken.ToString()).FirstOrDefault();
                if (_getUser == null)
                {
                    await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, $"شناسه کاربر نادرست است");
                    Proceed = false;
                }
                var _getProduct = db.Products.Find(ProductId);
                if (_getProduct == null)
                {
                    await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, $"شناسه محصول نادرست است");
                    Proceed = false;
                }
                if (Amount <= 0)
                {
                    await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, $"تعداد درخواستی معتبر نیست");
                    Proceed = false;
                }
                if (Amount > _getProduct.itemsAvailable)
                {
                    await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id, $"موجودی انبار کافی نیست");
                    Proceed = false;
                }
                if (Proceed == true)
                {
                    Cart ca = new Cart()
                    {
                        Insert_Date = DateTime.Now,
                        Product_Id = _getProduct.Id,
                        Quantity = Amount,
                        User_Id = _getUser.Id
                    };
                    db.Carts.Add(ca);
                    db.SaveChanges();


                    await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

                    await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

                    var keyboard = new ReplyKeyboardMarkup(new[]
   {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                        new KeyboardButton(Utilities.Enum.Home.Text)
                    }
            });
                    keyboard.ResizeKeyboard = true;

                    await Bot.SendTextMessageAsync(ChatId, "محصول " + _getProduct.Name + " به سبد خرید اضافه شد. خواستی میتونی ادامه بدی",
                        replyMarkup: keyboard);
                }
            }
        }
        public static async Task ViewCart(TelegramBotClient Bot, int UserToken)
        {
            await Bot.SendChatActionAsync(UserToken, ChatAction.Typing);
            string Cart = string.Empty;


            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getCart = db.Carts.Where(a => a.User.Token == UserToken.ToString()).ToList();
                if (!_getCart.Any())
                {
                    await Bot.SendChatActionAsync(UserToken, ChatAction.Typing);

                    var keyboard = new ReplyKeyboardMarkup(new[]
            {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                        new KeyboardButton(Utilities.Enum.Home.Text)
                    }
            });
                    keyboard.ResizeKeyboard = true;

                    await Bot.SendTextMessageAsync(UserToken, "سبد خرید ات خالیه، محصولات رو ببین شاید خوشت اومد",
                        replyMarkup: keyboard);
                }
                else
                {
                    foreach (var item in _getCart)
                    {
                        var sb = new StringBuilder();
                        sb.Append("محصول: " + item.Product.Name);
                        sb.Append(Environment.NewLine);
                        sb.Append("تعداد: " + item.Quantity + " عدد ");
                        sb.Append(Environment.NewLine);
                        sb.Append("قیمت: " + string.Format("{0:n0}", (item.Product.Price * item.Quantity)) + " تومان ");
                        sb.Append(Environment.NewLine);
                        //DeleteCartItem
                        sb.Append("حذف این آیتم از سبد خرید: " + String.Format(Logic.Utilities.Enum.DeleteCartItem.Command, item.Id)); sb.AppendLine("");

                        Cart = Cart + sb.ToString();
                    }


                    var keyboard = new InlineKeyboardMarkup(new[]
                             {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Utilities.Enum.Home.Text,Utilities.Enum.Home.Command),
                            InlineKeyboardButton.WithCallbackData(Utilities.Enum.ConfirmCart.Text,Utilities.Enum.ConfirmCart.Command)
                        }
                    });

                    await Bot.SendTextMessageAsync(UserToken, Cart,
                        replyMarkup: keyboard);
                }
            }
        }
        public static async Task DeleteCartItem(TelegramBotClient Bot, int ChatId, int CartId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getCart = db.Carts.Find(CartId);
                if (_getCart == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());

                db.Carts.Remove(_getCart);
                db.SaveChanges();

                await ViewCart(Bot, ChatId);
            }
        }
        public static async Task ConfirmCart(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                bool proceed = true;
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser != null)
                {
                    if (_getUser.Address == null)
                    {
                        _getUser.Last_Info_Status = Logic.Utilities.EnumLastActionStatus.WaitForAddress;
                        db.SaveChanges();

                        proceed = false;

                        var sb = new StringBuilder();
                        sb.Append("ازت آدرس ندارم، آدرستو بنویس برام");
                        sb.Append(Environment.NewLine);
                        sb.Append("دقت کن، ما سفارش رو به این آدرس میفرستیم");


                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
      replyMarkup: new ReplyKeyboardRemove());
                    }

                    else if (_getUser.Mobile == null)
                    {
                        _getUser.Last_Info_Status = Logic.Utilities.EnumLastActionStatus.WaitForMobile;
                        db.SaveChanges();

                        proceed = false;

                        var sb = new StringBuilder();
                        sb.Append("شماره ات رو هم ندارم که ! اونم بنویس");
                        sb.Append(Environment.NewLine);
                        sb.Append("بعد از تکمیل سفارش به این شماره زنگ میزنیم برای هماهنگی");

                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
      replyMarkup: new ReplyKeyboardRemove());
                    }
                }

                if (proceed == true)
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                                   {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Utilities.Enum.PayOnDelivery.Text,Utilities.Enum.PayOnDelivery.Command),
                            InlineKeyboardButton.WithCallbackData(Utilities.Enum.PayNow.Text,Utilities.Enum.PayNow.Command)
                        } });

                    await Bot.SendTextMessageAsync(ChatId, "مایلی پرداخت به چه صورت باشه؟",
          replyMarkup: keyboard);
                }
            }
        }
        public static async Task PayOnDelivery(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                bool proceed = true;
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser != null)
                {
                    if (_getUser.Address == null)
                    {
                        _getUser.Last_Info_Status = Logic.Utilities.EnumLastActionStatus.WaitForAddress;
                        db.SaveChanges();

                        proceed = false;

                        var sb = new StringBuilder();
                        sb.Append("ازت آدرس ندارم، آدرستو بنویس برام");
                        sb.Append(Environment.NewLine);
                        sb.Append("دقت کن، ما سفارش رو به این آدرس میفرستیم");

                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
      replyMarkup: new ReplyKeyboardRemove());
                    }

                    else if (_getUser.Mobile == null)
                    {
                        _getUser.Last_Info_Status = Logic.Utilities.EnumLastActionStatus.WaitForMobile;
                        db.SaveChanges();

                        proceed = false;

                        var sb = new StringBuilder();
                        sb.Append("شماره ات رو هم ندارم که ! اونم بنویس");
                        sb.Append(Environment.NewLine);
                        sb.Append("بعد از تکمیل سفارش به این شماره زنگ میزنیم برای هماهنگی");


                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
      replyMarkup: new ReplyKeyboardRemove());
                    }
                }


                if (proceed == true)
                {

                    Order ord = new Order()
                    {
                        Date = DateTime.Now,
                        Status = Utilities.EnumStatus.Paid,
                        UserId = _getUser.Id
                    };
                    db.Orders.Add(ord);
                    db.SaveChanges();

                    var _getCart = db.Carts.Where(a => a.User.Token == ChatId.ToString()).ToList();
                    foreach (var item in _getCart)
                    {
                        item.Product.itemsAvailable = item.Product.itemsAvailable - item.Quantity;
                        item.User.Last_Info_Status = Utilities.EnumLastActionStatus.None;
                        db.SaveChanges();

                        OrderItem or = new OrderItem()
                        {
                            Insert_Date = item.Insert_Date,
                            Product_Id = item.Product_Id,
                            Quantity = item.Quantity,
                            OrderId = ord.Id
                        };
                        db.OrderItems.Add(or);
                        db.SaveChanges();

                        db.Carts.Remove(item);
                        db.SaveChanges();
                    }

                    var keyboard = new InlineKeyboardMarkup(new[]
                                          {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Utilities.Enum.Home.Text,Utilities.Enum.Home.Command)
                        } });

                    await Bot.SendTextMessageAsync(ChatId, "سفارش شما ثبت شد، به همین زودیا بهتون خبر میدیم",
          replyMarkup: keyboard);
                }
            }
        }
        public static async Task PayNow(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                bool proceed = true;
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser != null)
                {
                    if (_getUser.Address == null)
                    {
                        _getUser.Last_Info_Status = Logic.Utilities.EnumLastActionStatus.WaitForAddress;
                        db.SaveChanges();

                        proceed = false;

                        var sb = new StringBuilder();
                        sb.Append("ازت آدرس ندارم، آدرستو بنویس برام");
                        sb.Append(Environment.NewLine);
                        sb.Append("دقت کن، ما سفارش رو به این آدرس میفرستیم");

                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
      replyMarkup: new ReplyKeyboardRemove());
                    }

                    else if (_getUser.Mobile == null)
                    {
                        _getUser.Last_Info_Status = Logic.Utilities.EnumLastActionStatus.WaitForMobile;
                        db.SaveChanges();

                        proceed = false;


                        var sb = new StringBuilder();
                        sb.Append("شماره ات رو هم ندارم که ! اونم بنویس");
                        sb.Append(Environment.NewLine);
                        sb.Append("بعد از تکمیل سفارش به این شماره زنگ میزنیم برای هماهنگی");


                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
      replyMarkup: new ReplyKeyboardRemove());
                    }
                }


                if (proceed == true)
                {

                    Order ord = new Order()
                    {
                        Date = DateTime.Now,
                        Status = Utilities.EnumStatus.Pending,
                        UserId = _getUser.Id
                    };
                    db.Orders.Add(ord);
                    db.SaveChanges();

                    int TotalPrice = 0;
                    var _getCart = db.Carts.Where(a => a.User.Token == ChatId.ToString()).ToList();
                    foreach (var item in _getCart)
                    {
                        item.Product.itemsAvailable = item.Product.itemsAvailable - item.Quantity;
                        item.User.Last_Info_Status = Utilities.EnumLastActionStatus.None;
                        db.SaveChanges();

                        TotalPrice += ((int)item.Product.Price * (int)item.Quantity);
                        OrderItem or = new OrderItem()
                        {
                            Insert_Date = item.Insert_Date,
                            Product_Id = item.Product_Id,
                            Quantity = item.Quantity,
                            OrderId = ord.Id
                        };
                        db.OrderItems.Add(or);
                        db.SaveChanges();

                        db.SaveChanges();
                    }

                    string url = "http://payment.si2ed.ir/api/Authorize/Get?OrderId=" + ord.Id + "&Price=" + TotalPrice + "&Store=" + ConfigurationManager.AppSettings["StoreName"];
                    string target = string.Empty;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";

                    try
                    {
                        using (WebResponse response = request.GetResponse())
                        {
                            string result = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            target = JsonConvert.DeserializeObject<string>(result);
                        }


                        var keyboard = new InlineKeyboardMarkup(new[]
                                          {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Logic.Utilities.Enum.Home.Text,Utilities.Enum.Home.Command),
                            InlineKeyboardButton.WithCallbackData(Logic.Utilities.Enum.Cart.Text,Utilities.Enum.Cart.Command)
                        } });


                        StringBuilder sb = new StringBuilder();
                        sb.Append("برای پرداخت روی لینک زیر کلیک کنید");
                        sb.Append(Environment.NewLine);
                        sb.Append(target);
                        sb.Append(Environment.NewLine);
                        sb.Append("با رعایت نکات امنیتی پرداخت کنید. بعد از پرداخت، همینجا تاییده رو میگیری.");


                        await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
              replyMarkup: keyboard);

                    }
                    catch (Exception ex)
                    {
                        await Bot.SendTextMessageAsync(ChatId, "پرداخت به مشکل خورد " + ex,
                                    replyMarkup: new ReplyKeyboardRemove());
                        await ConfirmCart(Bot, ChatId);
                    }
                }
            }
        }
        public static async Task CategoryList(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getCategory = db.Categories.Where(a => a.Products.Any(b => b.Category_Id == a.Id)).ToList();
                List<DynamicButtons> buttonItem = new List<DynamicButtons>();
                foreach (var item in _getCategory)
                {
                    DynamicButtons dbu = new DynamicButtons()
                    {
                        Command = string.Format(Utilities.Enum.CategoryProducts.Command, item.Id),
                        Text = item.Name
                    };
                    buttonItem.Add(dbu);
                }
                int row = (_getCategory.Count / 2) + 1;
                var keyboardMarkup = new InlineKeyboardMarkup(GetInlineKeyboard(2, row, buttonItem));

                await Bot.SendTextMessageAsync(ChatId, "یکی از دسته بندی ها رو انتخاب کن",
      replyMarkup: keyboardMarkup);
            }
        }
        public static async Task CategoryProducts(TelegramBotClient Bot, int ChatId, int CategoryId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.UploadPhoto);

            const string BaseAddress = @"C:\inetpub\vhosts\si2ed.ir\payment.si2ed.ir\WindowsService\Images\";

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _products = db.Products.Where(a => a.Category_Id == CategoryId && a.isActive == true && a.itemsAvailable > 0 && !a.Carts.Any(b => b.User.Token == ChatId.ToString()));
                if (!_products.Any())
                {
                    await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

                    var keyboard = new ReplyKeyboardMarkup(new[]
   {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                        new KeyboardButton(Utilities.Enum.Home.Text)
                    }
            });
                    keyboard.ResizeKeyboard = true;

                    await Bot.SendTextMessageAsync(ChatId, "شرمنده، محصولی محصولی توی این دسته بندی نیست",
                        replyMarkup: keyboard);
                }
                else
                {
                    foreach (var item in _products)
                    {
                        InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup();
                        string[] admin = ConfigurationManager.AppSettings["AdminToken"].Split('-');
                        if (admin.Any(a => a == ChatId.ToString()))
                        {
                            keyboard = new InlineKeyboardMarkup(new[]
                                                    {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.AddtoCart.Text,String.Format( Utilities.Enum.AddtoCart.Command,item.Id)),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Cart.Text,Utilities.Enum.Cart.Command),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Home.Text,Utilities.Enum.Home.Command)
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.EditProduct.Text,String.Format( Utilities.Enum.EditProduct.Command,item.Id))
                                }
                            });
                        }
                        else
                        {
                            keyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.AddtoCart.Text,String.Format( Utilities.Enum.AddtoCart.Command,item.Id)),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Cart.Text,Utilities.Enum.Cart.Command),
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.Home.Text,Utilities.Enum.Home.Command)
                                }
                            });

                        }
                        //image file
                        string file = BaseAddress + item.Image;
                        var fileName = file.Split('\\').Last();

                        //caption
                        var sb = new StringBuilder();
                        sb.Append("نام: " + item.Name);
                        sb.Append(Environment.NewLine);
                        sb.Append("توضیحات: " + item.Description);
                        sb.Append(Environment.NewLine);
                        sb.Append("قیمت: " + string.Format("{0:n0}", item.Price) + " تومان ");


                        await Task.Delay(500); // simulate longer running task

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var fts = new FileToSend(fileName, fileStream);

                            await Bot.SendPhotoAsync(ChatId, fts, sb.ToString(), false, 0, keyboard);
                        }
                    }
                    await Bot.SendTextMessageAsync(ChatId, "هر کدوم دوس داری بردار",
                 replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }
        public static async Task OpenOrders(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getOrders = db.Orders.Where(a => a.Status == Utilities.EnumStatus.Paid).ToList();

                foreach (var item in _getOrders)
                {
                    var sb = new StringBuilder();
                    sb.Append(string.Format("خریدار: {0} {1} @{2}", item.User.Name, item.User.Family, item.User.Username));
                    sb.Append(Environment.NewLine);
                    sb.Append(string.Format("شماره: {0}", item.User.Mobile));
                    sb.Append(Environment.NewLine);
                    sb.Append(string.Format("آدرس: {0}", item.User.Address));

                    foreach (var o in item.OrderItems)
                    {
                        sb.AppendLine("");

                        sb.AppendLine(string.Format("محصول: {0} {1} عدد", o.Product.Name, o.Quantity));
                        sb.AppendLine(string.Format("قیمت: {0}", o.Product.Price * o.Quantity));

                        sb.AppendLine("");
                    }

                    sb.Append("تکمیل سفارش: " + String.Format(Utilities.Enum.CompleteOrder.Command, item.Id)); sb.Append(Environment.NewLine);
                    sb.Append("لغو سفارش: " + String.Format(Utilities.Enum.CancelOrder.Command, item.Id)); sb.Append(Environment.NewLine);


                    await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
                        replyMarkup: new ReplyKeyboardRemove());
                }

                var keyboard = new ReplyKeyboardMarkup(new[]
{
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                        new KeyboardButton(Utilities.Enum.Home.Text)
                    }
            });
                keyboard.ResizeKeyboard = true;

                await Bot.SendTextMessageAsync(ChatId, "این همه سفارش های پرداخت شده هستن، باید باهاشون تماس بگیری",
                    replyMarkup: keyboard);
            }
        }
        public static async Task CancelOrder(TelegramBotClient Bot, int ChatId, int OrderId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getOrder = db.Orders.Find(OrderId);
                if (_getOrder == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    _getOrder.Status = Utilities.EnumStatus.Cancel;

                    foreach (var item in _getOrder.OrderItems)
                    {
                        item.Product.itemsAvailable = item.Product.itemsAvailable + item.Quantity;
                        db.SaveChanges();
                    }

                }
            }
            await OpenOrders(Bot, ChatId);
        }
        public static async Task CompleteOrder(TelegramBotClient Bot, int ChatId, int OrderId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getOrder = db.Orders.Find(OrderId);
                if (_getOrder == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    _getOrder.Status = Utilities.EnumStatus.Sent;
                    db.SaveChanges();
                }
            }
            await OpenOrders(Bot, ChatId);
        }
        public static async Task EditProduct(TelegramBotClient Bot, int ChatId, int ProductId, string ResponseMessage)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getProduct = db.Products.Find(ProductId);
                if (_getProduct == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                          {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("نام",String.Format( Utilities.Enum.EditProductAttributes.Command,_getProduct.Id,Utilities.EnumEditProductStatus.Name)),
                                    InlineKeyboardButton.WithCallbackData("قیمت",String.Format( Utilities.Enum.EditProductAttributes.Command,_getProduct.Id,Utilities.EnumEditProductStatus.Price)),
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("موجودی",String.Format( Utilities.Enum.EditProductAttributes.Command,_getProduct.Id,Utilities.EnumEditProductStatus.Quantity)),
                                    InlineKeyboardButton.WithCallbackData("توضیحات",String.Format( Utilities.Enum.EditProductAttributes.Command,_getProduct.Id,Utilities.EnumEditProductStatus.Description)),
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("تصویر",String.Format( Utilities.Enum.EditProductAttributes.Command,_getProduct.Id,Utilities.EnumEditProductStatus.Image)),
                                    InlineKeyboardButton.WithCallbackData("دسته بندی",String.Format( Utilities.Enum.EditProductAttributes.Command,_getProduct.Id,Utilities.EnumEditProductStatus.Category)),
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(Utilities.Enum.CancelEditProduct.Text,Utilities.Enum.CancelEditProduct.Command),
                                }
                            });
                    string Response = ResponseMessage != null ? ResponseMessage : "کدوم مورد رو میخوای ویرایش کنی؟";
                    await Bot.SendTextMessageAsync(ChatId, Response,
                    replyMarkup: keyboard);
                }
            }
        }
        public static async Task EditProductAttribute(TelegramBotClient Bot, int ChatId, int ProductId, int Action)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                                       {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(Utilities.Enum.CancelEditProduct.Text,Utilities.Enum.CancelEditProduct.Command)
                        } });

                    if (Action == Utilities.EnumEditProductStatus.Name)
                    {
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.Name;
                        _getUser.Current_Editing_Product = ProductId;

                        db.SaveChanges();

                        await Bot.SendTextMessageAsync(ChatId, "اسم جدید محصولت رو وارد کن",
                             replyMarkup: keyboard);
                    }
                    else if (Action == Utilities.EnumEditProductStatus.Price)
                    {
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.Price;
                        _getUser.Current_Editing_Product = ProductId; db.SaveChanges();

                        db.SaveChanges();

                        await Bot.SendTextMessageAsync(ChatId, "قیمت جدید محصولت رو وارد کن",
                             replyMarkup: keyboard);
                    }
                    else if (Action == Utilities.EnumEditProductStatus.Description)
                    {
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.Description;
                        _getUser.Current_Editing_Product = ProductId; db.SaveChanges();
                        db.SaveChanges();

                        await Bot.SendTextMessageAsync(ChatId, "توضیحات جدید محصولت رو وارد کن",
                             replyMarkup: keyboard);
                    }
                    else if (Action == Utilities.EnumEditProductStatus.Quantity)
                    {
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.Quantity;
                        _getUser.Current_Editing_Product = ProductId; db.SaveChanges();
                        db.SaveChanges();

                        await Bot.SendTextMessageAsync(ChatId, "موجودی جدید محصولت رو وارد کن",
                             replyMarkup: keyboard);
                    }
                    else if (Action == Utilities.EnumEditProductStatus.Category)
                    {
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.Category;
                        _getUser.Current_Editing_Product = ProductId; db.SaveChanges();
                        db.SaveChanges();

                        await Bot.SendTextMessageAsync(ChatId, "دسته بندی جدید محصولت رو وارد کن",
                             replyMarkup: keyboard);
                    }
                    else if (Action == Utilities.EnumEditProductStatus.Image)
                    {
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.Image;
                        _getUser.Current_Editing_Product = ProductId; db.SaveChanges();
                        db.SaveChanges();

                        await Bot.SendTextMessageAsync(ChatId, "تصویر جدید محصولت رو برام بفرست",
                             replyMarkup: keyboard);
                    }
                }
            }
        }
        public static async Task SaveEditProductAttribute(TelegramBotClient Bot, int ChatId, string Text, string FileId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    var _getProduct = db.Products.Find(_getUser.Current_Editing_Product);

                    if (_getUser.Last_Edit_Status == Utilities.EnumEditProductStatus.Name)
                    {
                        _getProduct.Name = Text;
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                        db.SaveChanges();

                        await Product(Bot, ChatId, _getProduct.Id);
                        await EditProduct(Bot, ChatId, _getProduct.Id, "نام محصولت ویرایش شد، حالا کدوم مورد رو میخوای ویرایش کنی؟");
                    }
                    if (_getUser.Last_Edit_Status == Utilities.EnumEditProductStatus.Price)
                    {
                        int n;
                        bool isNumeric = int.TryParse(Text, out n);
                        if (isNumeric)
                        {
                            _getProduct.Price = n;
                            _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                            db.SaveChanges();

                            await Product(Bot, ChatId, _getProduct.Id);
                            await EditProduct(Bot, ChatId, _getProduct.Id, "قیمت محصولت ویرایش شد، حالا کدوم مورد رو میخوای ویرایش کنی؟");
                        }
                        else
                        {
                            await EditProduct(Bot, ChatId, _getProduct.Id, "باید برای عدد قیمت وارد کنی، دوباره تلاش کن");
                        }
                    }
                    if (_getUser.Last_Edit_Status == Utilities.EnumEditProductStatus.Category)
                    {
                        var _getCategory = db.Categories.Where(a => a.Name == Text).FirstOrDefault();
                        if (_getCategory != null)
                        {
                            _getProduct.Name = Text;
                            db.SaveChanges();
                        }
                        else
                        {
                            Category c = new Category()
                            {
                                Name = Text
                            };
                            db.Categories.Add(c);
                            db.SaveChanges();

                            _getProduct.Category_Id = c.Id;
                            _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                            db.SaveChanges();
                        }

                        await Product(Bot, ChatId, _getProduct.Id);
                        await EditProduct(Bot, ChatId, _getProduct.Id, "دسته بندی محصولت ویرایش شد، حالا کدوم مورد رو میخوای ویرایش کنی؟");
                    }
                    if (_getUser.Last_Edit_Status == Utilities.EnumEditProductStatus.Quantity)
                    {
                        int n;
                        bool isNumeric = int.TryParse(Text, out n);
                        if (isNumeric)
                        {
                            _getProduct.itemsAvailable = n;
                            _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                            db.SaveChanges();

                            await Product(Bot, ChatId, _getProduct.Id);
                            await EditProduct(Bot, ChatId, _getProduct.Id, "موجودی محصولت ویرایش شد، حالا کدوم مورد رو میخوای ویرایش کنی؟");
                        }
                        else
                        {
                            await EditProduct(Bot, ChatId, _getProduct.Id, "باید برای تعداد موجودی عدد وارد کنی، دوباره تلاش کن");
                        }
                    }
                    if (_getUser.Last_Edit_Status == Utilities.EnumEditProductStatus.Description)
                    {
                        _getProduct.Description = Text;
                        _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                        db.SaveChanges();

                        await Product(Bot, ChatId, _getProduct.Id);
                        await EditProduct(Bot, ChatId, _getProduct.Id, "دسته بندی محصولت ویرایش شد، حالا کدوم مورد رو میخوای ویرایش کنی؟");
                    }
                    if (_getUser.Last_Edit_Status == Utilities.EnumEditProductStatus.Image)
                    {
                        if (!String.IsNullOrWhiteSpace(FileId))
                        {
                            const string BaseAddress = @"C:\inetpub\vhosts\si2ed.ir\payment.si2ed.ir\WindowsService\Images\";
                            var filePath = Path.Combine(BaseAddress, FileId + ".jpg");

                            using (var file = System.IO.File.OpenWrite(filePath))
                            {
                                await Bot.GetFileAsync(FileId, file);
                            }

                            _getProduct.Image = FileId + ".jpg";
                            _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                            db.SaveChanges();

                            await Product(Bot, ChatId, _getProduct.Id);
                            await EditProduct(Bot, ChatId, _getProduct.Id, "تصویر محصولت ویرایش شد، حالا کدوم مورد رو میخوای ویرایش کنی؟");
                        }
                        else
                        {
                            await EditProduct(Bot, ChatId, _getProduct.Id, "محصولت عکس میخواد، دوباره تلاش کن");
                        }
                    }


                }
            }
            await OpenOrders(Bot, ChatId);
        }
        public static async Task CancelEditProduct(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    _getUser.Last_Edit_Status = Utilities.EnumEditProductStatus.None;
                    db.SaveChanges();
                }
            }
            await HomePage(Bot, ChatId);
        }
        public static async Task AddProduct(TelegramBotClient Bot, int ChatId)
        {
            await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == ChatId.ToString()).FirstOrDefault();
                if (_getUser == null)
                    await Bot.SendTextMessageAsync(ChatId, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    _getUser.Insert_Product_Mode = Utilities.EnumInsertModeStatus.Yes;
                    db.SaveChanges();

                    var sb = new StringBuilder();
                    sb.AppendLine("برای اضافه کردن محصول، یه عکس برام بفرست که همون عکس محصوله");
                    sb.AppendLine("بعد، دقیقا به این ترتیب که میگم اطلاعات رو بفرست");
                    sb.AppendLine("نام محصول");
                    sb.AppendLine("دسته بندی");
                    sb.AppendLine("قیمت (عدد)");
                    sb.AppendLine("موجودی انبار (عد)");

                    await Bot.SendTextMessageAsync(ChatId, sb.ToString(),
                    replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }
        public static async Task SaveProduct(TelegramBotClient Bot, Message message)
        {
            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            using (GoldoonEntities db = new GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == message.Chat.Id.ToString()).FirstOrDefault();
                if (_getUser == null)
                    await Bot.SendTextMessageAsync(message.Chat.Id, "شناسه آیتم ارسالی نادرست است",
                    replyMarkup: new ReplyKeyboardRemove());
                else
                {
                    string[] lines = message.Caption.Split('\n');

                    int Price;
                    int Quantity;
                    string Category;
                    string Name;
                    if (lines.Count() < 4 || int.TryParse(lines[2], out Price) == false || int.TryParse(lines[3], out Quantity) == false)
                    {
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                    },
                      new []
                    {
                          new KeyboardButton(Utilities.Enum.OpenOrders.Text),
                          new KeyboardButton(Utilities.Enum.AddProduct.Text)
                    }
            });
                        keyboard.ResizeKeyboard = true;

                        await Bot.SendTextMessageAsync(message.Chat.Id, "لطفا دستور العمل رو درست اجرا کن",
                            replyMarkup: keyboard);
                    }
                    else
                    {
                        string FileId = string.Empty;
                        if (message.Document != null && message.Document.MimeType.Contains("image"))
                        {
                            FileId = message.Document.FileId;
                        }
                        else if (message.Photo != null)
                        {
                            FileId = message.Photo.LastOrDefault().FileId;
                        }

                        if (string.IsNullOrWhiteSpace(FileId))
                        {
                            var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                    new []
                    {
                        new KeyboardButton(Utilities.Enum.Cart.Text),
                        new KeyboardButton(Utilities.Enum.CategoryList.Text),
                    },
                      new []
                    {
                          new KeyboardButton(Utilities.Enum.OpenOrders.Text),
                          new KeyboardButton(Utilities.Enum.AddProduct.Text)
                    }
            });
                            keyboard.ResizeKeyboard = true;

                            await Bot.SendTextMessageAsync(message.Chat.Id, "لطفا دستور العمل رو درست اجرا کن",
                                replyMarkup: keyboard);
                        }
                        else
                        {
                            const string BaseAddress = @"C:\inetpub\vhosts\si2ed.ir\payment.si2ed.ir\WindowsService\Images\";
                            var filePath = Path.Combine(BaseAddress, FileId + ".jpg");

                            using (var file = System.IO.File.OpenWrite(filePath))
                            {
                                await Bot.GetFileAsync(FileId, file);
                            }

                            int CatId = 0;
                            Category = lines[1];
                            var _getCategory = db.Categories.Where(a => a.Name == Category).FirstOrDefault();
                            if (_getCategory == null)
                            {
                                Category ca = new Category() { Name = Category };
                                db.Categories.Add(ca);
                                db.SaveChanges();

                                CatId = ca.Id;
                            }
                            else
                                CatId = _getCategory.Id;

                            Name = lines[0];

                            Product pr = new Product()
                            {
                                Image = FileId + ".jpg",
                                isActive = true,
                                Name = Name,
                                itemsAvailable = Quantity,
                                Price = Price,
                                Category_Id = CatId
                            };

                            db.Products.Add(pr);
                            db.SaveChanges();

                            await Product(Bot, (int)message.Chat.Id, pr.Id);
                            _getUser.Insert_Product_Mode = Utilities.EnumInsertModeStatus.No;
                            db.SaveChanges();

                            await HomePage(Bot, (int)message.Chat.Id);
                        }
                    }
                }
            }
        }
    }
}
