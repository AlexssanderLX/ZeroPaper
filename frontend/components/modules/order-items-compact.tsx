"use client";

import { useState } from "react";
import { formatCurrency } from "@/components/modules/module-utils";
import type { OrderItem } from "@/lib/api";

function OrderItemCompactImage({ item }: { item: OrderItem }) {
  const [hasError, setHasError] = useState(false);

  if (!item.imageUrl || hasError) {
    return (
      <div className="order-item-compact-thumb order-item-compact-thumb-placeholder" aria-hidden="true">
        <span>{item.name.slice(0, 1)}</span>
      </div>
    );
  }

  return (
    <img
      className="order-item-compact-thumb"
      src={item.imageUrl}
      alt={item.name}
      loading="lazy"
      onError={() => setHasError(true)}
    />
  );
}

export function OrderItemsCompact({ items }: { items: OrderItem[] }) {
  if (!items.length) {
    return null;
  }

  return (
    <div className="order-item-compact-list">
      {items.map((item) => (
        <article key={item.id} className="order-item-compact-card">
          <OrderItemCompactImage item={item} />

          <div className="order-item-compact-copy">
            <div className="order-item-compact-head">
              {item.categoryName ? <span className="order-item-compact-category">{item.categoryName}</span> : null}
              <strong>{item.name}</strong>
            </div>
            {item.notes ? <p>Obs: {item.notes}</p> : null}
          </div>

          <div className="order-item-compact-meta">
            <span>{item.quantity}x</span>
            <strong>{formatCurrency(item.totalPrice)}</strong>
          </div>
        </article>
      ))}
    </div>
  );
}
