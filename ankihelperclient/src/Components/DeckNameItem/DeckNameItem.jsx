import { useId } from 'react';
import styles from './DeckNameItem.module.css';
import { useLanguage } from '../../i18n/useLanguage';

const DeckNameItem = ({ name, isAlreadyAdded = false, isSelected, onSelectionChange }) => {
    const id = useId();
    const { t } = useLanguage();

    return (
        <li className={styles.deckNameItem}>
            <input
                className={styles.checkbox}
                id={id}
                type="checkbox"
                checked={isSelected}
                onChange={({ target }) => onSelectionChange(name, target.checked)}
            />
            <label className={styles.label} htmlFor={id}>
                {name}
            </label>
            {isAlreadyAdded && (
                <span className={styles.alreadyAddedMark} title={t("addDecks.alreadyAdded")} aria-label={t("addDecks.alreadyAdded")}>
                    {"\u2713"}
                </span>
            )}
        </li>
    );
};

export default DeckNameItem;
