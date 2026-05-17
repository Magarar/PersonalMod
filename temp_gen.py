import shutil, os

src = os.path.join(os.path.dirname(__file__), "PersonalMod", "Debu999PersonalMod", "images", "card_portraits", "card.png")
dst = os.path.join(os.path.dirname(__file__), "PersonalMod", "Debu999PersonalMod", "images", "card_portraits", "chaos_orb.png")
shutil.copy2(src, dst)
print("done")
