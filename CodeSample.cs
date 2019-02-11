
public static List<Transaction> OrderForAggregating(this List<Transaction> pTransactions)
{
	pTransactions = pTransactions.OrderBy(o => o.TransactionType)
		.ThenBy(o => o.SyndicateId)
		.ThenBy(o => o.MerchantId)
		.ThenBy(o => o.ManagementPartnerId)
		.ThenBy(o => o.OffPlatformPayeeId)
		.ThenBy(o => o.SalesAgentId)
		.ThenBy(o => o.FundingPartnerId)
		.ToList();
	return pTransactions;
}
		
///////////////////////////		 
public static List<GeneralLedger> ToGeneralLedgerRows(List<Transaction> pTransactions)
{
	List<GeneralLedger> rGeneralLedgerRows = new List<GeneralLedger>();

	for (int i = 0; i < pTransactions.Count; i++)
	{
		int NumberOfPairedTransactions = 0;
		//If has value, then its already been processed. This could happen because of the ordering method. 
		if (!pTransactions[i].GeneralLedgerId.HasValue)
		{
			//Do not aggregate
			if (pTransactions[i].TransactionType != TransactionType.MerchantDebit && pTransactions[i].TransactionType != TransactionType.SyndicateDebit &&
				pTransactions[i].TransactionType != TransactionType.SalesAgentCredit && pTransactions[i].TransactionType != TransactionType.SalesAgentDebit)
			{
				//Check the Next transaction.
				while ((i+1+NumberOfPairedTransactions < pTransactions.Count) &&
					pTransactions[i + 1 + NumberOfPairedTransactions].TransactionType == pTransactions[i].TransactionType)
				{
					if (pTransactions[i].FundingPartnerId == pTransactions[i + 1 + NumberOfPairedTransactions].FundingPartnerId &&
							pTransactions[i].ManagementPartnerId == pTransactions[i + 1 + NumberOfPairedTransactions].ManagementPartnerId &&
							pTransactions[i].MerchantId == pTransactions[i + 1 + NumberOfPairedTransactions].MerchantId &&
							pTransactions[i].OffPlatformPayeeId == pTransactions[i + 1 + NumberOfPairedTransactions].OffPlatformPayeeId &&
							pTransactions[i].SalesAgentId == pTransactions[i + 1 + NumberOfPairedTransactions].SalesAgentId &&
							pTransactions[i].SyndicateId == pTransactions[i + 1 + NumberOfPairedTransactions].SyndicateId)
						NumberOfPairedTransactions++;
					else
						break;
				}
			}
			rGeneralLedgerRows.AddRange(pTransactions.GetRange(i, (1 + NumberOfPairedTransactions)).ToGeneralLedgerRows());

			//What if the General Ledger Row was negative?
			foreach (var glr in rGeneralLedgerRows.Where(x => x.TransactionType == TransactionType.SyndicateCredit && x.Amount < 0).ToList())
			{
				glr.TransactionType = TransactionType.SyndicateDebit;
				glr.Amount = -(glr.Amount);
				glr.Notes += " Negative Credit Reversed";
				glr.AddOrUpdate();
			}

			rGeneralLedgerRows.AddOrUpdate();
			//The following sub loop moves over all transactions involved in the General Ledger that was created
			for (int j = 0; j < (NumberOfPairedTransactions+1); j++)
			{
				pTransactions[i+j].GeneralLedgerId = rGeneralLedgerRows.Last().GeneralLedgerId;
				pTransactions[i+j].InitiatedDateTime = DateTime.Now;
				pTransactions[i+j].AddOrUpdate();
			}
		}
	}
	return rGeneralLedgerRows;
}