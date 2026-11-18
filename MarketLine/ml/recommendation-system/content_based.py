"""Content-based product recommendations.

Builds a TF-IDF vector from name, description, brand, category and features,
then ranks other products by cosine similarity.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import joblib
import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity

ROOT = Path(__file__).resolve().parent
DATA_PATH = ROOT / "data" / "products.csv"
MODEL_PATH = ROOT / "models" / "content_based.joblib"

STOP_WORDS = {
    "за", "с", "и", "на", "от", "до", "при", "без", "да", "се", "в", "във",
    "е", "са", "или", "като", "със", "по", "към", "а", "но", "че", "този",
    "тази", "тези", "подходящ", "подходящи", "лек", "лека",
}


def product_text(row: pd.Series) -> str:
    name = str(row.get("name") or "")
    description = str(row.get("description") or "")
    brand = str(row.get("brand") or "")
    category = str(row.get("category") or "")
    features = str(row.get("features") or "").replace(",", " ")
    # Repeat structured fields so category/brand outweigh filler words.
    parts = [name, description, brand, brand, brand, category, category, category, features, features]
    return " ".join(parts).strip().lower()


def train(data_path: Path = DATA_PATH, model_path: Path = MODEL_PATH) -> None:
    products = pd.read_csv(data_path)
    if products.empty:
        raise ValueError("products.csv is empty")

    products["content"] = products.apply(product_text, axis=1)
    vectorizer = TfidfVectorizer(min_df=1, ngram_range=(1, 2), stop_words=list(STOP_WORDS))
    matrix = vectorizer.fit_transform(products["content"])
    model_path.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(
        {
            "vectorizer": vectorizer,
            "matrix": matrix,
            "products": products.drop(columns=["content"]),
        },
        model_path,
    )
    print(f"Saved model: {model_path}")


def recommend(item_id: int, k: int = 4, model_path: Path = MODEL_PATH) -> pd.DataFrame:
    if not model_path.exists():
        raise FileNotFoundError("Train first: python content_based.py train")

    bundle = joblib.load(model_path)
    products: pd.DataFrame = bundle["products"]
    matrix = bundle["matrix"]
    match = products.index[products["id"] == item_id]
    if len(match) == 0:
        raise ValueError(f"Unknown product id: {item_id}")

    row = match[0]
    scores = cosine_similarity(matrix[row], matrix).flatten()
    ranked = scores.argsort()[::-1]
    picks = [i for i in ranked if i != row and scores[i] >= 0.05][:k]
    result = products.iloc[picks].copy()
    result["score"] = scores[picks]
    return result[["id", "name", "category", "brand", "score"]]


def main() -> None:
    parser = argparse.ArgumentParser(description="Content-based recommender")
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("train", help="Fit TF-IDF and save models/content_based.joblib")

    rec = sub.add_parser("recommend", help="Show similar products")
    rec.add_argument("--item-id", type=int, required=True)
    rec.add_argument("-k", type=int, default=4)

    args = parser.parse_args()
    if args.command == "train":
        train()
        return

    print(recommend(args.item_id, args.k).to_string(index=False))


if __name__ == "__main__":
    main()
