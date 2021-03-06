/* 風覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧藍
   | ﾎ�褞璋韋 �琅 ���鉅���                                                                                    |
   | ﾏ�������� 糂� ��褞璋韋 �琅 ���鉅��� �� �瑕�� �粱����� ��褞璋���� �琅 ����褊���, ���鉅�� � ��� �瑰�� CDS. |
   |                                                                                                          |
   | 1.  ﾏ����頸� ���鉅�� 韈 ��頌�� ���鉅� (韈 矜鍄).                                                         |
   | 2.  ﾏ����褊韃 ������ �� ���鉅��                                                                          |
   | 3.  ﾄ�矜粱褊韃 蒟�裙 �� ��褪                                                                             |
   | 4.  ﾏ��襌褊韃 � ���鉅�� �裝鞨��褌褊��. ﾏ瑩瑟褪��: idMe, idSellType, ���韲����                            |
   | 5.  ﾏ������ ﾌﾝ 韈 ���鉅��: 肄�褞頏�籵�韃 ������ �� ��璞鞣瑙韃 (CDS), ��頌瑙韃 蒟�裙 �� ��褪�             |
   | 6.  ﾕ�瑙褊韃 ��頌�� ������ �� ��璞鞣瑙韃 �裝鞨��褌褊��, 頌���韋 鈞�珸�� 珮��褊��                         |
   |                                                                                                          |
   |                                                                                           Roman Blinkov  |
   風覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧覧藍 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Goods;
using log4net;
using Rai;
using Raidb;

namespace CDS {
  /// <summary>
  /// ﾊ��鉅��
  /// </summary>
  public class Basket {
    static readonly object locker = new object();

    int id;

    int idBasketCollection = (int) BasketCollectionEnum.main;
    protected ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    string msisdn;

    Basket() {
    }

    /// <summary>
    /// If basket can't be found creates new basket 
    /// </summary>
    /// <param name="MSISDN"></param>
    public Basket(string MSISDN)
      : this(MSISDN, (int) BasketCollectionEnum.main) {
    }

    public Basket(string MSISDN, int idBasketCollection) {
      lock (locker) {
        using (var db = new DbDataContext()) {
          msisdn = MSISDN;
          this.idBasketCollection = idBasketCollection;
          AbonentBasket b = null;
          if ("" != MSISDN) //temp basket
          {
            int count = db.Abonents.Count(t => t.MSISDN == MSISDN);
            if (0 == count) {
              //adding new abonent and new basket abonent
              db.Abonents.InsertOnSubmit(new Abonent {MSISDN = MSISDN});
              db.SubmitChanges();
            }
            b = db.AbonentBaskets.SingleOrDefault(
              m => m.MSISDN == MSISDN && m.idBasketCollection == idBasketCollection);
          }

          if (b == null) {
            b = new AbonentBasket {
                                    MSISDN = MSISDN,
                                    balans = 0,
                                    idBasketCollection = idBasketCollection,
                                    hash = Guid.NewGuid().ToString()
                                  };
            db.AbonentBaskets.InsertOnSubmit(b);
            db.SubmitChanges();
            id = b.id;
          }
          else {
            id = b.id;
            msisdn = MSISDN;
            this.idBasketCollection = idBasketCollection;
          }
        }
      }
    }

    public int Id {
      get { return id; }
    }

    public List<BasketItem> BasketItems {
      get {
        var ret = new List<BasketItem>();
        using (var db = new DbDataContext())
          foreach (Raidb.BasketItem a in db.AbonentBaskets.SingleOrDefault(t => t.id == id).BasketItems) {
            ret.Add(new BasketItem(a));
          }
        return ret;
      }
    }

    //decimal balans;
    public decimal Balans {
      get {
        using (var db = new DbDataContext())
          return db.AbonentBaskets.Single(t => t.id == id).balans;
      }
      set {
        using (var db = new DbDataContext())
          db.AbonentBaskets.Single(t => t.id == id).balans = value;
      }
    }

    public string Hash {
      get {
        using (var db = new DbDataContext())
          return db.AbonentBaskets.Single(t => t.id == id).hash;
      }
    }

    public string MSISDN {
      get { return msisdn; }
    }

    public string Info {
      get {
        using (var db = new DbDataContext()) {
          AbonentBasket a = db.AbonentBaskets.SingleOrDefault(t => t.id == id);
          if (null != a)
            return string.Format("id: {4} idBasketCollection {0}, MSISDN: {1} Balans: {2} Items count {3}",
                                 new object[]
                                 {idBasketCollection, msisdn, Balans, a.BasketItems.Count(), id});
          else {
            string e = string.Format("Can not find Basket in database. AbonentBaskets.id = {0}", id);
            log.Error(e);
            throw new NullReferenceException(e);
          }
        }
      }
    }

    /// <summary>
    /// Adds BasketItem into basket
    /// No operations with money
    /// </summary>
    public void AddBasketItem(int idMe, SellTypeEnum sellType, decimal Price) {
      var newBasketItem = new BasketItem(id, idMe, sellType, Price);
    }

    public static Basket GetNewTempBasket(int idBasketCollection) {
      return new Basket("", idBasketCollection);
    }

    public static Basket GetBasketByHash(string hash) {
      using (var db = new DbDataContext()) {
        AbonentBasket b = db.AbonentBaskets.SingleOrDefault(t => t.hash == hash);
        return null != b ? new Basket {id = b.id, idBasketCollection = b.idBasketCollection, msisdn = b.MSISDN} : null;
      }
    }

    public static Basket GetBasketById(int id) {
      using (var db = new DbDataContext()) {
        AbonentBasket b = db.AbonentBaskets.SingleOrDefault(t => t.id == id);
        return null == b ? null : new Basket();
      }
    }

    /// <summary>
    /// Warning!
    /// This method modifies basket passed as argument and deletes it!
    /// </summary>
    /// <param name="a">Basket that must be added to current Basket</param>
    public void Combine(ref Basket a) {
      using (var db = new DbDataContext()) {
        // ���鉅�� 砒銖����褊�� ���琅�籵�� �瑟� � ��碚�                  
        if (id == a.id)
          return;
        log.InfoFormat("Combining Basket {0} with {1}", Info, a.Info);
        //ﾁ琿瑙�
        Balans += a.Balans;
        //ﾎ磅裝竟�褌 ��蒟�跖��� ���鉅�. ﾍ� 蔘硴韭瑣� ���� 鈞礪籵褌.
        int count = db.ExecuteCommand(
          string.Format(@"UPDATE BasketItems SET idAbonentBaskets = {0} WHERE idAbonentBaskets = {1}",
                        id, a.id));
        log.InfoFormat(
          "Succesfully combined. Result {0} with {1}. {3} records affected. Deleting second Basket",
          new object[] {Info, a.Info, count});
        a.Delete();
        db.SubmitChanges();
      }
    }

    public void Delete() {
      using (var db = new DbDataContext()) {
        AbonentBasket b = db.AbonentBaskets.SingleOrDefault(t => t.id == id);
        if (null == b) return;
        db.AbonentBaskets.DeleteOnSubmit(b);
        db.SubmitChanges();
      }
    }
  }
}