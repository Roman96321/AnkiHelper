import styles from './Button.module.css'

const Button = ({ className = '', type = 'button', children, onClick, isDisabled }) => {
    return (
        <button
            className={`${styles.button} ${className}`}
            type={type}
            onClick={onClick}
            disabled={isDisabled}
        >{children}</button>
    )
}

export default Button;