using GoGuido.Models;
using Microsoft.AspNetCore.Identity;

namespace GoGuido.Data;

public static class SeedData
{
    public static void Run(AppDbContext db, IPasswordHasher<User> h)
    {
        if (db.Cities.Any()) return;

        var cityDefs = new (string, string, string, string, int)[]
        {
            ("Agra", "Uttar Pradesh", "Home of the Taj Mahal — the Mughal capital of marble, red sandstone and eternal love stories.", "🕌", 28),
            ("Delhi", "Delhi", "Seven cities in one: Sultanate ruins, Mughal grandeur and British-era boulevards.", "🏛️", 8),
            ("Jaipur", "Rajasthan", "The Pink City — Rajput forts, royal palaces and astronomical wonders.", "🏰", 340),
            ("Fatehpur Sikri", "Uttar Pradesh", "Akbar's abandoned red-sandstone capital, frozen in time since the 1580s.", "🕍", 15),
            ("Varanasi", "Uttar Pradesh", "One of the world's oldest living cities, alive with ghats, temples and Ganga aarti.", "🪔", 38),
            ("Udaipur", "Rajasthan", "The City of Lakes — white marble palaces floating over Lake Pichola.", "🏯", 200),
            ("Lucknow", "Uttar Pradesh", "City of Nawabs: Awadhi architecture, Imambaras and tehzeeb.", "🕋", 160),
            ("Amritsar", "Punjab", "The spiritual heart of Sikhism, glowing with the golden Harmandir Sahib.", "🛕", 45),
        };
        var cities = cityDefs.Select(c => new City { Name = c.Item1, State = c.Item2, Description = c.Item3, Icon = c.Item4, Hue = c.Item5 }).ToList();
        db.Cities.AddRange(cities);
        db.SaveChanges();
        int C(string n) => cities.First(c => c.Name == n).Id;

        // city, name, era, approx fee INR (Indian citizens), unesco, icon, description
        var m = new (string, string, string, int, bool, string, string)[]
        {
            ("Agra","Taj Mahal","1632–1653 · Shah Jahan",50,true,"🕌","The ivory-white marble mausoleum built for Mumtaz Mahal — a masterpiece of Mughal architecture."),
            ("Agra","Agra Fort","1565–1573 · Akbar",40,true,"🏰","A massive red sandstone fortress and palace complex on the Yamuna, with views of the Taj."),
            ("Agra","Itmad-ud-Daulah's Tomb","1622–1628 · Nur Jahan",30,false,"🕍","The 'Baby Taj' — the first Mughal tomb in white marble with exquisite pietra dura inlay."),
            ("Agra","Mehtab Bagh","1530s · Babur",30,false,"🌳","A moonlit Mughal garden across the river, perfect for sunset views of the Taj."),
            ("Agra","Akbar's Tomb, Sikandra","1605–1613",30,false,"🕌","The grand tomb of Emperor Akbar set in a vast garden with red sandstone gateways."),
            ("Fatehpur Sikri","Buland Darwaza","1601 · Akbar",50,true,"🚪","At 54 metres, one of the tallest gateways in the world, built to celebrate Gujarat's conquest."),
            ("Fatehpur Sikri","Panch Mahal","c. 1570s",50,true,"🏯","A five-storey open pavilion of columns where the royal court enjoyed cool breezes."),
            ("Fatehpur Sikri","Jama Masjid, Fatehpur Sikri","c. 1571",0,true,"🕌","One of the largest mosques in India, a blend of Persian and Indian styles."),
            ("Fatehpur Sikri","Salim Chishti's Tomb","1580–1581",0,true,"🕍","A shimmering white marble shrine with delicate jali screens inside the mosque courtyard."),
            ("Fatehpur Sikri","Jodha Bai's Palace","c. 1570s",50,true,"🏰","The largest royal residence in the complex, mixing Hindu and Mughal motifs."),
            ("Delhi","Red Fort (Lal Qila)","1639–1648 · Shah Jahan",35,true,"🏰","The imposing red sandstone fort from where the PM hoists the flag every Independence Day."),
            ("Delhi","Qutub Minar","1193 · Qutb ud-Din Aibak",35,true,"🗼","A 73-metre victory tower of red sandstone — the tallest brick minaret in the world."),
            ("Delhi","Humayun's Tomb","1565–1572",35,true,"🕌","The garden-tomb that inspired the Taj Mahal, and India's first of its kind."),
            ("Delhi","India Gate","1921–1931 · Edwin Lutyens",0,false,"🏛️","A 42-metre war memorial arch honouring soldiers of the British Indian Army."),
            ("Delhi","Jama Masjid, Delhi","1650–1656 · Shah Jahan",0,false,"🕌","India's largest mosque, with a courtyard for 25,000 worshippers."),
            ("Delhi","Lotus Temple","1986",0,false,"🪷","A flowerlike Bahá'í House of Worship open to all, in white marble petals."),
            ("Delhi","Jantar Mantar, Delhi","1724 · Jai Singh II",15,false,"🧭","An 18th-century observatory of giant masonry astronomical instruments."),
            ("Jaipur","Amber Fort","1592 · Raja Man Singh I",100,true,"🏰","A hilltop fort of sandstone and marble with the dazzling Sheesh Mahal mirror hall."),
            ("Jaipur","Hawa Mahal","1799 · Sawai Pratap Singh",50,false,"🌸","The 'Palace of Winds' with 953 small windows in a honeycomb pink façade."),
            ("Jaipur","City Palace, Jaipur","1729–1732",200,false,"🏛️","A royal complex blending Rajput, Mughal and European architecture."),
            ("Jaipur","Jantar Mantar, Jaipur","1734 · Jai Singh II",50,true,"🧭","The world's largest stone sundial and a UNESCO-listed astronomical observatory."),
            ("Jaipur","Nahargarh Fort","1734",50,false,"🌇","A fort on the Aravalli ridge, famous for its sunset views over Jaipur."),
            ("Jaipur","Jal Mahal","18th century",0,false,"🏝️","A palace seemingly floating in the middle of Man Sagar Lake."),
            ("Varanasi","Dashashwamedh Ghat","18th century rebuilt",0,false,"🪔","The most spectacular ghat, home to the nightly Ganga Aarti ceremony."),
            ("Varanasi","Kashi Vishwanath Temple","1780 · Ahilyabai Holkar",0,false,"🛕","One of the twelve Jyotirlingas — the golden-spired heart of Varanasi."),
            ("Varanasi","Sarnath","3rd c. BCE · Ashoka",30,false,"☸️","Where Buddha gave his first sermon; home of the Dhamek Stupa."),
            ("Udaipur","City Palace, Udaipur","1553 · Maharana Udai Singh II",300,false,"🏯","A grand palace complex on Lake Pichola with balconies, courtyards and museums."),
            ("Udaipur","Lake Pichola","1362",0,false,"⛵","A serene artificial lake with Jag Mandir and Lake Palace on its islands."),
            ("Udaipur","Sajjangarh Monsoon Palace","1884",250,false,"⛰️","A hilltop palace with panoramic views of the lakes and the Aravallis."),
            ("Lucknow","Bara Imambara","1784 · Asaf-ud-Daula",50,false,"🕌","A vast Nawabi complex with the famed Bhul Bhulaiya labyrinth."),
            ("Lucknow","Rumi Darwaza","1784",0,false,"🚪","A 60-foot gateway often called the Turkish Gate, the emblem of Lucknow."),
            ("Lucknow","Chota Imambara","1838 · Muhammad Ali Shah",20,false,"✨","The 'Palace of Lights' with chandeliers and a gilded dome."),
            ("Amritsar","Golden Temple (Harmandir Sahib)","1604 · Guru Arjan",0,false,"🛕","The holiest Sikh gurdwara, with its gold-plated sanctum in the Amrit Sarovar."),
            ("Amritsar","Jallianwala Bagh","1919 memorial",0,false,"🕯️","A memorial garden commemorating the 1919 massacre during the freedom struggle."),
        };
        db.Monuments.AddRange(m.Select(x => new Monument
        {
            CityId = C(x.Item1), Name = x.Item2, Era = x.Item3, EntryFeeInr = x.Item4,
            IsUnesco = x.Item5, Icon = x.Item6, Description = x.Item7
        }));

        User Mk(string name, string email, string pw, string role, string? phone = null)
        {
            var u = new User { FullName = name, Email = email, Role = role, Phone = phone };
            u.PasswordHash = h.HashPassword(u, pw);
            return u;
        }
        var admin = Mk("GoGuido Admin", "admin@goguido.in", "Admin@123", Roles.Admin);
        var tourist = Mk("Demo Tourist", "tourist@goguido.in", "Tourist@123", Roles.Tourist, "9876543210");
        db.Users.AddRange(admin, tourist);

        var g = new (string, string, string, int, int, string, string)[]
        {
            ("Agra","Rajesh Sharma","rajesh@goguido.in",14,800,"English,Hindi,French","Govt-licensed Taj Mahal specialist. Sunrise tours and Mughal history storytelling."),
            ("Agra","Fatima Khan","fatima@goguido.in",9,700,"English,Hindi,Urdu","Agra Fort & Itmad-ud-Daulah expert with a love for Mughal poetry."),
            ("Fatehpur Sikri","Salman Ansari","salman@goguido.in",11,600,"English,Hindi,Urdu","Born and raised near Fatehpur Sikri; deep knowledge of Akbar's court."),
            ("Delhi","Anil Verma","anil@goguido.in",16,900,"English,Hindi,German","Red Fort, Qutub Minar and Old Delhi heritage walks with food stops."),
            ("Delhi","Sunita Rao","sunita@goguido.in",8,750,"English,Hindi,Spanish","Delhi's Sultanate and Mughal layers, Humayun's Tomb to Lodhi Gardens."),
            ("Jaipur","Vikram Singh Shekhawat","vikram@goguido.in",18,1000,"English,Hindi,Rajasthani","Amber Fort, royal Rajput lineage stories and hidden Jaipur forts."),
            ("Jaipur","Meera Joshi","meera@goguido.in",7,700,"English,Hindi,French","Hawa Mahal, City Palace & craft bazaars — art and textiles focus."),
            ("Varanasi","Ravi Mishra","ravi@goguido.in",20,650,"English,Hindi,Sanskrit","Ganga aarti, ghat boat rides, and temple lore from a Varanasi native."),
            ("Udaipur","Kiran Mehta","kiran@goguido.in",10,850,"English,Hindi,Gujarati","City Palace, lakes and Mewar history with sunset boat plans."),
            ("Lucknow","Imran Naqvi","imran@goguido.in",12,600,"English,Hindi,Urdu","Imambaras, Awadhi tehzeeb and the Tunday Kababi food trail."),
            ("Amritsar","Harpreet Kaur","harpreet@goguido.in",9,600,"English,Hindi,Punjabi","Golden Temple, langar experience and Jallianwala Bagh history."),
        };
        var rnd = new Random(7);
        var comments = new[] { "Brilliant storyteller — we learnt so much!", "Very knowledgeable and punctual.", "Made our family trip unforgettable.", "Great photo spots and local tips." };
        db.SaveChanges();
        foreach (var x in g)
        {
            var u = Mk(x.Item2, x.Item3, "Guide@123", Roles.Guide);
            var gp = new GuideProfile
            {
                User = u, CityId = C(x.Item1), ExperienceYears = x.Item4, PricePerHour = x.Item5,
                Languages = x.Item6, Bio = x.Item7, Status = St.Approved, LicenseNumber = "GL-" + rnd.Next(10000, 99999)
            };
            db.GuideProfiles.Add(gp);
            for (int i = 0; i < 3; i++)
            {
                var bk = new Booking { Tourist = tourist, GuideProfile = gp, TourDate = DateTime.UtcNow.AddDays(-10 - i), Hours = 4, TotalPrice = x.Item5 * 4, Status = St.Completed };
                db.Bookings.Add(bk);
                db.Reviews.Add(new Review { Booking = bk, GuideProfile = gp, Tourist = tourist, Rating = rnd.Next(4, 6), Comment = comments[rnd.Next(comments.Length)] });
            }
        }
        db.SaveChanges();
    }
}
